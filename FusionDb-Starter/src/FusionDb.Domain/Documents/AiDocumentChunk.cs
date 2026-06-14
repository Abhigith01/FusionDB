using System.Security.Cryptography;
using System.Text;
using NpgsqlTypes;
using Pgvector;

namespace FusionDb.Domain.Documents;

public sealed class AiDocumentChunk
{
    private AiDocumentChunk() { }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public int ChunkNumber { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public string ContentHash { get; private set; } = string.Empty;

    public int StartOffset { get; private set; }

    public int EndOffset { get; private set; }

    public Vector? Embedding { get; private set; }

    public NpgsqlTsVector SearchVector { get; private set; } = null!;

    public string? EmbeddingModel { get; private set; }

    public DateTimeOffset? EmbeddedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static AiDocumentChunk Create(
        Guid documentId,
        int chunkNumber,
        string content,
        int startOffset,
        int endOffset
    )
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document ID is required.", nameof(documentId));
        }

        if (chunkNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkNumber),
                "Chunk number must be greater than zero."
            );
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Chunk content is required.", nameof(content));
        }

        if (startOffset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startOffset),
                "Start offset cannot be negative."
            );
        }

        if (endOffset <= startOffset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endOffset),
                "End offset must be greater than start offset."
            );
        }

        var normalizedContent = content.Trim();

        return new AiDocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ChunkNumber = chunkNumber,
            Content = normalizedContent,
            ContentHash = CalculateHash(normalizedContent),
            StartOffset = startOffset,
            EndOffset = endOffset,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SetEmbedding(float[] values, string model, int expectedDimensions)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length != expectedDimensions)
        {
            throw new ArgumentException(
                $"Embedding contains {values.Length} dimensions, "
                    + $"but the collection expects {expectedDimensions}.",
                nameof(values)
            );
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Embedding model is required.", nameof(model));
        }

        Embedding = new Vector(values);
        EmbeddingModel = model.Trim();
        EmbeddedAt = DateTimeOffset.UtcNow;
    }

    private static string CalculateHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
