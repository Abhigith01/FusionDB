namespace FusionDb.Domain.Collections;

public sealed class AiCollection
{
    private AiCollection()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int VectorDimensions { get; private set; }

    public string EmbeddingModel { get; private set; } = string.Empty;

    public int ChunkSize { get; private set; }

    public int ChunkOverlap { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static AiCollection Create(
        string name,
        string? description,
        int vectorDimensions,
        string embeddingModel,
        int chunkSize,
        int chunkOverlap)
    {
        Validate(
            name,
            vectorDimensions,
            embeddingModel,
            chunkSize,
            chunkOverlap);

        var currentTime = DateTimeOffset.UtcNow;

        return new AiCollection
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim(),
            VectorDimensions = vectorDimensions,
            EmbeddingModel = embeddingModel.Trim(),
            ChunkSize = chunkSize,
            ChunkOverlap = chunkOverlap,
            CreatedAt = currentTime,
            UpdatedAt = currentTime
        };
    }

    private static void Validate(
        string name,
        int vectorDimensions,
        string embeddingModel,
        int chunkSize,
        int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Collection name is required.",
                nameof(name));
        }

        if (name.Trim().Length > 200)
        {
            throw new ArgumentException(
                "Collection name cannot exceed 200 characters.",
                nameof(name));
        }

        if (vectorDimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vectorDimensions),
                "Vector dimensions must be greater than zero.");
        }

        if (vectorDimensions > 16_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vectorDimensions),
                "Vector dimensions cannot exceed 16,000.");
        }

        if (string.IsNullOrWhiteSpace(embeddingModel))
        {
            throw new ArgumentException(
                "Embedding model is required.",
                nameof(embeddingModel));
        }

        if (embeddingModel.Trim().Length > 200)
        {
            throw new ArgumentException(
                "Embedding model cannot exceed 200 characters.",
                nameof(embeddingModel));
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkSize),
                "Chunk size must be greater than zero.");
        }

        if (chunkOverlap < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkOverlap),
                "Chunk overlap cannot be negative.");
        }

        if (chunkOverlap >= chunkSize)
        {
            throw new ArgumentException(
                "Chunk overlap must be smaller than chunk size.",
                nameof(chunkOverlap));
        }
    }
}