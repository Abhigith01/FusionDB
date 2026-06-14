namespace FusionDb.Application.Observability;

public interface IRetrievalAuditService
{
    Task<Guid> RecordAsync(
        RetrievalAuditInput input,
        CancellationToken cancellationToken = default
    );
}

public sealed record RetrievalAuditInput(
    Guid CollectionId,
    string Operation,
    string QueryText,
    string? MetadataFilterJson,
    double MinimumSimilarity,
    int RequestedLimit,
    int ResultCount,
    long DurationMilliseconds,
    string? GenerationModel,
    string? Answer,
    bool? Grounded,
    string ResultsJson,
    string Status,
    string? ErrorMessage = null
);
