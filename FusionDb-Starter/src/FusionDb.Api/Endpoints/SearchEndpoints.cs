using System.Text.Json;
using FusionDb.Api.Contracts;
using FusionDb.Api.Contracts.Search;
using FusionDb.Application.Embeddings;
using FusionDb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace FusionDb.Api.Endpoints;

public static class SearchEndpoints
{
    private const double SemanticWeight = 0.65;
    private const double KeywordWeight = 0.35;
    private const double ReciprocalRankConstant = 60.0;

    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/api/collections/{collectionId:guid}/search", SearchCollectionAsync)
            .WithTags("AI Search");

        return endpoints;
    }

    private static async Task<IResult> SearchCollectionAsync(
        Guid collectionId,
        SearchCollectionRequest request,
        FusionDbContext dbContext,
        IEmbeddingGenerator embeddingGenerator,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest(new ErrorResponse("Search query is required."));
        }

        if (request.Limit is < 1 or > 50)
        {
            return Results.BadRequest(new ErrorResponse("Search limit must be between 1 and 50."));
        }

        if (request.MinimumSimilarity is < 0 or > 1)
        {
            return Results.BadRequest(
                new ErrorResponse("Minimum similarity must be between 0 and 1.")
            );
        }

        string? metadataFilterJson = null;

        if (request.MetadataFilter is { } metadataFilter)
        {
            if (metadataFilter.ValueKind != JsonValueKind.Object)
            {
                return Results.BadRequest(
                    new ErrorResponse("Metadata filter must be a JSON object.")
                );
            }

            if (metadataFilter.EnumerateObject().Any())
            {
                metadataFilterJson = metadataFilter.GetRawText();
            }
        }

        var collection = await dbContext
            .AiCollections.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == collectionId, cancellationToken);

        if (collection is null)
        {
            return Results.NotFound(
                new ErrorResponse($"Collection '{collectionId}' was not found.")
            );
        }

        var queryText = request.Query.Trim();

        IReadOnlyList<float[]> generatedEmbeddings;

        try
        {
            generatedEmbeddings = await embeddingGenerator.GenerateAsync(
                collection.EmbeddingModel,
                new[] { queryText },
                cancellationToken
            );
        }
        catch (Exception exception)
        {
            return Results.Problem(
                title: "Query embedding generation failed",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }

        if (generatedEmbeddings.Count != 1)
        {
            return Results.Problem(
                title: "Query embedding generation failed",
                detail: $"Expected one embedding but received " + $"{generatedEmbeddings.Count}.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }

        var embeddingValues = generatedEmbeddings[0];

        if (embeddingValues.Length != collection.VectorDimensions)
        {
            return Results.Problem(
                title: "Embedding dimension mismatch",
                detail: $"The query embedding contains "
                    + $"{embeddingValues.Length} dimensions, but "
                    + $"the collection expects "
                    + $"{collection.VectorDimensions}.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }

        var queryVector = new Vector(embeddingValues);

        var candidateLimit = Math.Clamp(request.Limit * 10, 20, 100);

        /*
         * Semantic/vector candidates.
         */
        var semanticRows = await (
            from chunk in dbContext.AiDocumentChunks.AsNoTracking()
            join document in dbContext.AiDocuments.AsNoTracking()
                on chunk.DocumentId equals document.Id
            where
                document.CollectionId == collectionId
                && chunk.Embedding != null
                && (
                    metadataFilterJson == null
                    || EF.Functions.JsonContains(document.MetadataJson, metadataFilterJson)
                )
            orderby chunk.Embedding!.CosineDistance(queryVector)
            select new
            {
                ChunkId = chunk.Id,
                DocumentId = document.Id,
                DocumentTitle = document.Title,
                chunk.ChunkNumber,
                chunk.Content,
                Distance = chunk.Embedding!.CosineDistance(queryVector),
            }
        )
            .Take(candidateLimit)
            .ToListAsync(cancellationToken);

        /*
         * Keyword/full-text candidates.
         */
        var keywordRows = await (
            from chunk in dbContext.AiDocumentChunks.AsNoTracking()
            join document in dbContext.AiDocuments.AsNoTracking()
                on chunk.DocumentId equals document.Id
            where
                document.CollectionId == collectionId
                && chunk.Embedding != null
                && (
                    metadataFilterJson == null
                    || EF.Functions.JsonContains(document.MetadataJson, metadataFilterJson)
                )
                && chunk.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", queryText))
            orderby chunk.SearchVector.RankCoverDensity(
                EF.Functions.WebSearchToTsQuery("english", queryText)
            ) descending
            select new
            {
                ChunkId = chunk.Id,
                DocumentId = document.Id,
                DocumentTitle = document.Title,
                chunk.ChunkNumber,
                chunk.Content,
                Distance = chunk.Embedding!.CosineDistance(queryVector),
                KeywordScore = chunk.SearchVector.RankCoverDensity(
                    EF.Functions.WebSearchToTsQuery("english", queryText)
                ),
            }
        )
            .Take(candidateLimit)
            .ToListAsync(cancellationToken);

        var candidates = new Dictionary<Guid, HybridCandidate>();

        /*
         * Add semantic candidates only when they pass
         * the minimum-similarity threshold.
         */
        var semanticRank = 0;

        foreach (var row in semanticRows)
        {
            var similarity = 1.0 - row.Distance;

            if (similarity < request.MinimumSimilarity)
            {
                break;
            }

            semanticRank++;

            candidates[row.ChunkId] = new HybridCandidate
            {
                ChunkId = row.ChunkId,
                DocumentId = row.DocumentId,
                DocumentTitle = row.DocumentTitle,
                ChunkNumber = row.ChunkNumber,
                Content = row.Content,
                Distance = row.Distance,
                Similarity = similarity,
                SemanticRank = semanticRank,
            };
        }

        /*
         * Add keyword candidates even when semantic
         * similarity is below the threshold.
         */
        for (var index = 0; index < keywordRows.Count; index++)
        {
            var row = keywordRows[index];
            var keywordRank = index + 1;
            var similarity = 1.0 - row.Distance;

            if (!candidates.TryGetValue(row.ChunkId, out var candidate))
            {
                candidate = new HybridCandidate
                {
                    ChunkId = row.ChunkId,
                    DocumentId = row.DocumentId,
                    DocumentTitle = row.DocumentTitle,
                    ChunkNumber = row.ChunkNumber,
                    Content = row.Content,
                    Distance = row.Distance,
                    Similarity = similarity,
                };

                candidates.Add(row.ChunkId, candidate);
            }

            candidate.KeywordRank = keywordRank;
            candidate.KeywordScore = Convert.ToDouble(row.KeywordScore);
        }

        /*
         * Reciprocal Rank Fusion.
         */
        var maximumPossibleScore =
            (SemanticWeight + KeywordWeight) / (ReciprocalRankConstant + 1.0);

        var results = candidates
            .Values.Select(candidate =>
            {
                var rawScore = 0.0;

                if (candidate.SemanticRank.HasValue)
                {
                    rawScore +=
                        SemanticWeight / (ReciprocalRankConstant + candidate.SemanticRank.Value);
                }

                if (candidate.KeywordRank.HasValue)
                {
                    rawScore +=
                        KeywordWeight / (ReciprocalRankConstant + candidate.KeywordRank.Value);
                }

                var hybridScore = maximumPossibleScore == 0 ? 0 : rawScore / maximumPossibleScore;

                return new SearchResultResponse(
                    candidate.ChunkId,
                    candidate.DocumentId,
                    candidate.DocumentTitle,
                    candidate.ChunkNumber,
                    candidate.Content,
                    Math.Round(candidate.Distance, 6),
                    Math.Round(candidate.Similarity, 6),
                    Math.Round(candidate.KeywordScore, 6),
                    candidate.SemanticRank,
                    candidate.KeywordRank,
                    Math.Round(hybridScore, 6)
                );
            })
            .OrderByDescending(result => result.HybridScore)
            .ThenByDescending(result => result.Similarity)
            .Take(request.Limit)
            .ToList();

        return Results.Ok(new SearchCollectionResponse(queryText, results.Count, results));
    }

    private sealed class HybridCandidate
    {
        public Guid ChunkId { get; init; }

        public Guid DocumentId { get; init; }

        public string DocumentTitle { get; init; } = string.Empty;

        public int ChunkNumber { get; init; }

        public string Content { get; init; } = string.Empty;

        public double Distance { get; init; }

        public double Similarity { get; init; }

        public double KeywordScore { get; set; }

        public int? SemanticRank { get; set; }

        public int? KeywordRank { get; set; }
    }
}
