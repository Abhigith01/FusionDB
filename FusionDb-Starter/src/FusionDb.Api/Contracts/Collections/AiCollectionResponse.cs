using FusionDb.Domain.Collections;

namespace FusionDb.Api.Contracts.Collections;

public sealed record AiCollectionResponse(
    Guid Id,
    string Name,
    string? Description,
    int VectorDimensions,
    string EmbeddingModel,
    int ChunkSize,
    int ChunkOverlap,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static AiCollectionResponse FromEntity(
        AiCollection collection)
    {
        return new AiCollectionResponse(
            collection.Id,
            collection.Name,
            collection.Description,
            collection.VectorDimensions,
            collection.EmbeddingModel,
            collection.ChunkSize,
            collection.ChunkOverlap,
            collection.CreatedAt,
            collection.UpdatedAt);
    }
}