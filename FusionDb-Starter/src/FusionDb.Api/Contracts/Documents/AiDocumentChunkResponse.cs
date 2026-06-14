using FusionDb.Domain.Documents;

namespace FusionDb.Api.Contracts.Documents;

public sealed record AiDocumentChunkResponse(
    Guid Id,
    Guid DocumentId,
    int ChunkNumber,
    string Content,
    string ContentHash,
    int StartOffset,
    int EndOffset,
    DateTimeOffset CreatedAt
)
{
    public static AiDocumentChunkResponse FromEntity(AiDocumentChunk chunk)
    {
        return new AiDocumentChunkResponse(
            chunk.Id,
            chunk.DocumentId,
            chunk.ChunkNumber,
            chunk.Content,
            chunk.ContentHash,
            chunk.StartOffset,
            chunk.EndOffset,
            chunk.CreatedAt
        );
    }
}
