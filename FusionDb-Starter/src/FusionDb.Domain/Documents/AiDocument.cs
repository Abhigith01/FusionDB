using System.Security.Cryptography;
using System.Text;

namespace FusionDb.Domain.Documents;

public sealed class AiDocument
{
    private AiDocument() { }

    public Guid Id { get; private set; }

    public Guid CollectionId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public string MetadataJson { get; private set; } = "{}";

    public string ContentHash { get; private set; } = string.Empty;

    public DocumentStatus Status { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static AiDocument Create(
        Guid collectionId,
        string title,
        string content,
        string? metadataJson
    )
    {
        if (collectionId == Guid.Empty)
        {
            throw new ArgumentException("Collection ID is required.", nameof(collectionId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Document title is required.", nameof(title));
        }

        if (title.Trim().Length > 500)
        {
            throw new ArgumentException(
                "Document title cannot exceed 500 characters.",
                nameof(title)
            );
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Document content is required.", nameof(content));
        }

        var normalizedContent = content.Trim();
        var currentTime = DateTimeOffset.UtcNow;

        return new AiDocument
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            Title = title.Trim(),
            Content = normalizedContent,
            MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson,
            ContentHash = CalculateHash(normalizedContent),
            Status = DocumentStatus.Pending,
            CreatedAt = currentTime,
            UpdatedAt = currentTime,
        };
    }

    public void MarkProcessing()
    {
        Status = DocumentStatus.Processing;
        FailureReason = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkReady()
    {
        Status = DocumentStatus.Ready;
        FailureReason = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string failureReason)
    {
        Status = DocumentStatus.Failed;

        FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? "Document processing failed."
            : failureReason.Trim();

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string CalculateHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
