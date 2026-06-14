using FusionDb.Application.Embeddings;
using FusionDb.Application.Search;
using FusionDb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace FusionDb.Infrastructure.Search;

public sealed class HybridSearchService : IHybridSearchService
{
    private const double SemanticWeight = 0.65;
    private const double KeywordWeight = 0.35;
    private const double ReciprocalRankConstant = 60.0;

    private readonly FusionDbContext _dbContext;
    private readonly IEmbeddingGenerator _embeddingGenerator;

    public HybridSearchService(FusionDbContext dbContext, IEmbeddingGenerator embeddingGenerator)
    {
        _dbContext = dbContext;
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<HybridSearchResult> SearchAsync(
        Guid collectionId,
        string query,
        int limit,
        double minimumSimilarity,
        string? metadataFilterJson,
        CancellationToken cancellationToken = default
    )
    {
        var collection = await _dbContext
            .AiCollections.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == collectionId, cancellationToken);

        if (collection is null)
        {
            return new HybridSearchResult(false, Array.Empty<HybridSearchHit>());
        }

        var queryText = query.Trim();

        var generatedEmbeddings = await _embeddingGenerator.GenerateAsync(
            collection.EmbeddingModel,
            new[] { queryText },
            cancellationToken
        );

        if (generatedEmbeddings.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected one query embedding but received " + $"{generatedEmbeddings.Count}."
            );
        }

        var embeddingValues = generatedEmbeddings[0];

        if (embeddingValues.Length != collection.VectorDimensions)
        {
            throw new InvalidOperationException(
                $"The query embedding contains "
                    + $"{embeddingValues.Length} dimensions, but the "
                    + $"collection expects "
                    + $"{collection.VectorDimensions}."
            );
        }

        var queryVector = new Vector(embeddingValues);

        var candidateLimit = Math.Clamp(limit * 10, 20, 100);

        var semanticRows = await (
            from chunk in _dbContext.AiDocumentChunks.AsNoTracking()
            join document in _dbContext.AiDocuments.AsNoTracking()
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

        var keywordRows = await (
            from chunk in _dbContext.AiDocumentChunks.AsNoTracking()
            join document in _dbContext.AiDocuments.AsNoTracking()
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

        var semanticRank = 0;

        foreach (var row in semanticRows)
        {
            var similarity = 1.0 - row.Distance;

            if (similarity < minimumSimilarity)
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

        var maximumPossibleScore =
            (SemanticWeight + KeywordWeight) / (ReciprocalRankConstant + 1.0);

        var hits = candidates
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

                var hybridScore = rawScore / maximumPossibleScore;

                return new HybridSearchHit(
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
            .OrderByDescending(hit => hit.HybridScore)
            .ThenByDescending(hit => hit.Similarity)
            .Take(limit)
            .ToList();

        return new HybridSearchResult(true, hits);
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
