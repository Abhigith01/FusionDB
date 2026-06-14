namespace FusionDb.Api.Contracts.Documents;

public sealed record ProcessDocumentResponse(Guid DocumentId, int ChunkCount, string Status);
