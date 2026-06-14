namespace FusionDb.Domain.Observability;

public sealed class RetrievalAudit
{
    private RetrievalAudit() { }

    public Guid Id { get; private set; }

    public Guid CollectionId { get; private set; }

    public string Operation { get; private set; } = string.Empty;

    public string QueryText { get; private set; } = string.Empty;

    public string MetadataFilterJson { get; private set; } = "{}";

    public double MinimumSimilarity { get; private set; }

    public int RequestedLimit { get; private set; }

    public int ResultCount { get; private set; }

    public long DurationMilliseconds { get; private set; }

    public string? GenerationModel { get; private set; }

    public string? Answer { get; private set; }

    public bool? Grounded { get; private set; }

    public string ResultsJson { get; private set; } = "[]";

    public string Status { get; private set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static RetrievalAudit Create(
        Guid collectionId,
        string operation,
        string queryText,
        string? metadataFilterJson,
        double minimumSimilarity,
        int requestedLimit,
        int resultCount,
        long durationMilliseconds,
        string? generationModel,
        string? answer,
        bool? grounded,
        string resultsJson,
        string status,
        string? errorMessage = null
    )
    {
        if (collectionId == Guid.Empty)
        {
            throw new ArgumentException("Collection ID is required.", nameof(collectionId));
        }

        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException("Operation is required.", nameof(operation));
        }

        if (string.IsNullOrWhiteSpace(queryText))
        {
            throw new ArgumentException("Query text is required.", nameof(queryText));
        }

        if (requestedLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedLimit));
        }

        if (resultCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(resultCount));
        }

        if (durationMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
        }

        if (string.IsNullOrWhiteSpace(resultsJson))
        {
            resultsJson = "[]";
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.", nameof(status));
        }

        return new RetrievalAudit
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            Operation = operation.Trim(),
            QueryText = queryText.Trim(),
            MetadataFilterJson = string.IsNullOrWhiteSpace(metadataFilterJson)
                ? "{}"
                : metadataFilterJson,
            MinimumSimilarity = minimumSimilarity,
            RequestedLimit = requestedLimit,
            ResultCount = resultCount,
            DurationMilliseconds = durationMilliseconds,
            GenerationModel = generationModel?.Trim(),
            Answer = answer?.Trim(),
            Grounded = grounded,
            ResultsJson = resultsJson,
            Status = status.Trim(),
            ErrorMessage = errorMessage?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
