using System.Text.Json;

namespace FusionDb.Api.Contracts.Observability;

public sealed record RetrievalAuditSummaryResponse(
    Guid Id,
    Guid CollectionId,
    string Operation,
    string QueryText,
    int ResultCount,
    long DurationMilliseconds,
    string? GenerationModel,
    bool? Grounded,
    string Status,
    DateTimeOffset CreatedAt
);

public sealed record RetrievalAuditDetailResponse(
    Guid Id,
    Guid CollectionId,
    string Operation,
    string QueryText,
    JsonElement MetadataFilter,
    double MinimumSimilarity,
    int RequestedLimit,
    int ResultCount,
    long DurationMilliseconds,
    string? GenerationModel,
    string? Answer,
    bool? Grounded,
    JsonElement Results,
    string Status,
    string? ErrorMessage,
    DateTimeOffset CreatedAt
);

public sealed record PagedRetrievalAuditsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<RetrievalAuditSummaryResponse> Items
);

public sealed class RetrievalAuditQuery
{
    public string? Operation { get; init; }

    public string? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
