namespace FusionDb.Api.Contracts.Collections;

public sealed record CreateAiCollectionRequest(
    string Name,
    string? Description,
    int VectorDimensions,
    string EmbeddingModel,
    int ChunkSize,
    int ChunkOverlap);