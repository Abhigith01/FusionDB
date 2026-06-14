using System.Diagnostics;
using System.Text.Json;
using FusionDb.Api.Contracts;
using FusionDb.Api.Contracts.Search;
using FusionDb.Application.Observability;
using FusionDb.Application.Search;

namespace FusionDb.Api.Endpoints;

public static class SearchEndpoints
{
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
        IHybridSearchService hybridSearchService,
        IRetrievalAuditService retrievalAuditService,
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

        if (request.MetadataFilter is { } filter)
        {
            if (filter.ValueKind != JsonValueKind.Object)
            {
                return Results.BadRequest(
                    new ErrorResponse("Metadata filter must be a JSON object.")
                );
            }

            if (filter.EnumerateObject().Any())
            {
                metadataFilterJson = filter.GetRawText();
            }
        }

        var stopwatch = Stopwatch.StartNew();

        HybridSearchResult searchResult;

        try
        {
            searchResult = await hybridSearchService.SearchAsync(
                collectionId,
                request.Query.Trim(),
                request.Limit,
                request.MinimumSimilarity,
                metadataFilterJson,
                cancellationToken
            );
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            return Results.Problem(
                title: "Search failed",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }

        if (!searchResult.CollectionFound)
        {
            stopwatch.Stop();

            return Results.NotFound(
                new ErrorResponse($"Collection '{collectionId}' was not found.")
            );
        }

        var results = searchResult
            .Hits.Select(hit => new SearchResultResponse(
                hit.ChunkId,
                hit.DocumentId,
                hit.DocumentTitle,
                hit.ChunkNumber,
                hit.Content,
                hit.Distance,
                hit.Similarity,
                hit.KeywordScore,
                hit.SemanticRank,
                hit.KeywordRank,
                hit.HybridScore
            ))
            .ToList();

        stopwatch.Stop();

        try
        {
            await retrievalAuditService.RecordAsync(
                new RetrievalAuditInput(
                    collectionId,
                    Operation: "search",
                    QueryText: request.Query.Trim(),
                    MetadataFilterJson: metadataFilterJson,
                    MinimumSimilarity: request.MinimumSimilarity,
                    RequestedLimit: request.Limit,
                    ResultCount: results.Count,
                    DurationMilliseconds: stopwatch.ElapsedMilliseconds,
                    GenerationModel: null,
                    Answer: null,
                    Grounded: null,
                    ResultsJson: JsonSerializer.Serialize(results),
                    Status: results.Count == 0 ? "NoResults" : "Succeeded"
                ),
                cancellationToken
            );
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Search audit logging failed: " + $"{exception.Message}");
        }

        return Results.Ok(
            new SearchCollectionResponse(request.Query.Trim(), results.Count, results)
        );
    }
}
