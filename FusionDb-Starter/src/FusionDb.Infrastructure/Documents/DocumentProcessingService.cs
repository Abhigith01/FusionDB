using FusionDb.Application.Chunking;
using FusionDb.Application.Documents;
using FusionDb.Application.Embeddings;
using FusionDb.Domain.Documents;
using FusionDb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FusionDb.Infrastructure.Documents;

public sealed class DocumentProcessingService : IDocumentProcessingService
{
    private readonly FusionDbContext _dbContext;
    private readonly ITextChunker _textChunker;
    private readonly IEmbeddingGenerator _embeddingGenerator;

    public DocumentProcessingService(
        FusionDbContext dbContext,
        ITextChunker textChunker,
        IEmbeddingGenerator embeddingGenerator
    )
    {
        _dbContext = dbContext;
        _textChunker = textChunker;
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<DocumentProcessingResult> ProcessAsync(
        Guid collectionId,
        Guid documentId,
        CancellationToken cancellationToken = default
    )
    {
        var collection = await _dbContext.AiCollections.FirstOrDefaultAsync(
            item => item.Id == collectionId,
            cancellationToken
        );

        if (collection is null)
        {
            return new DocumentProcessingResult(
                CollectionFound: false,
                DocumentFound: false,
                documentId,
                ChunkCount: 0,
                Status: "NotFound"
            );
        }

        var document = await _dbContext.AiDocuments.FirstOrDefaultAsync(
            item => item.Id == documentId && item.CollectionId == collectionId,
            cancellationToken
        );

        if (document is null)
        {
            return new DocumentProcessingResult(
                CollectionFound: true,
                DocumentFound: false,
                documentId,
                ChunkCount: 0,
                Status: "NotFound"
            );
        }

        document.MarkProcessing();
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var generatedChunks = _textChunker.Chunk(
                document.Content,
                collection.ChunkSize,
                collection.ChunkOverlap
            );

            if (generatedChunks.Count == 0)
            {
                throw new InvalidOperationException("The document produced no searchable chunks.");
            }

            var chunkContents = generatedChunks.Select(chunk => chunk.Content).ToArray();

            var generatedEmbeddings = await _embeddingGenerator.GenerateAsync(
                collection.EmbeddingModel,
                chunkContents,
                cancellationToken
            );

            if (generatedEmbeddings.Count != generatedChunks.Count)
            {
                throw new InvalidOperationException(
                    "The number of generated embeddings does not "
                        + "match the number of document chunks."
                );
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                cancellationToken
            );

            var existingChunks = await _dbContext
                .AiDocumentChunks.Where(chunk => chunk.DocumentId == documentId)
                .ToListAsync(cancellationToken);

            _dbContext.AiDocumentChunks.RemoveRange(existingChunks);

            for (var index = 0; index < generatedChunks.Count; index++)
            {
                var generatedChunk = generatedChunks[index];
                var generatedEmbedding = generatedEmbeddings[index];

                var chunk = AiDocumentChunk.Create(
                    document.Id,
                    generatedChunk.Number,
                    generatedChunk.Content,
                    generatedChunk.StartOffset,
                    generatedChunk.EndOffset
                );

                chunk.SetEmbedding(
                    generatedEmbedding,
                    collection.EmbeddingModel,
                    collection.VectorDimensions
                );

                _dbContext.AiDocumentChunks.Add(chunk);
            }

            document.MarkReady();

            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new DocumentProcessingResult(
                CollectionFound: true,
                DocumentFound: true,
                document.Id,
                generatedChunks.Count,
                document.Status.ToString()
            );
        }
        catch (Exception exception)
        {
            _dbContext.ChangeTracker.Clear();

            var failedDocument = await _dbContext.AiDocuments.FirstOrDefaultAsync(
                item => item.Id == documentId,
                cancellationToken
            );

            if (failedDocument is not null)
            {
                failedDocument.MarkFailed(exception.Message);

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            throw;
        }
    }
}
