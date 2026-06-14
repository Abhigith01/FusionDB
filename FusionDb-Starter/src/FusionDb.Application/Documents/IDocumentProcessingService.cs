namespace FusionDb.Application.Documents;

public interface IDocumentProcessingService
{
    Task<DocumentProcessingResult> ProcessAsync(
        Guid collectionId,
        Guid documentId,
        CancellationToken cancellationToken = default
    );
}

public sealed record DocumentProcessingResult(
    bool CollectionFound,
    bool DocumentFound,
    Guid DocumentId,
    int ChunkCount,
    string Status
);
