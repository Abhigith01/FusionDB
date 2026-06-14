namespace FusionDb.Api.Contracts.Documents;

public sealed record UploadDocumentResponse(
    Guid DocumentId,
    string Title,
    string FileName,
    long SizeBytes,
    int? PageCount,
    int ChunkCount,
    string Status
);
