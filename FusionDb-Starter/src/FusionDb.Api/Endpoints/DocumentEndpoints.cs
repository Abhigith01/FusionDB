using FusionDb.Api.Contracts;
using FusionDb.Api.Contracts.Documents;
using FusionDb.Application.Chunking;
using FusionDb.Application.Embeddings;
using FusionDb.Domain.Documents;
using FusionDb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FusionDb.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/collections/{collectionId:guid}/documents")
            .WithTags("AI Documents");

        group.MapPost("/", CreateDocumentAsync);
        group.MapGet("/", GetDocumentsAsync);
        group.MapGet("/{documentId:guid}", GetDocumentByIdAsync);

        group.MapPost("/{documentId:guid}/process", ProcessDocumentAsync);

        group.MapGet("/{documentId:guid}/chunks", GetDocumentChunksAsync);

        return endpoints;
    }

    private static async Task<IResult> ProcessDocumentAsync(
        Guid collectionId,
        Guid documentId,
        FusionDbContext dbContext,
        ITextChunker textChunker,
        IEmbeddingGenerator embeddingGenerator,
        CancellationToken cancellationToken
    )
    {
        var collection = await dbContext.AiCollections.FirstOrDefaultAsync(
            item => item.Id == collectionId,
            cancellationToken
        );

        if (collection is null)
        {
            return Results.NotFound(
                new ErrorResponse($"Collection '{collectionId}' was not found.")
            );
        }

        var document = await dbContext.AiDocuments.FirstOrDefaultAsync(
            item => item.Id == documentId && item.CollectionId == collectionId,
            cancellationToken
        );

        if (document is null)
        {
            return Results.NotFound(new ErrorResponse($"Document '{documentId}' was not found."));
        }

        document.MarkProcessing();

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var generatedChunks = textChunker.Chunk(
                document.Content,
                collection.ChunkSize,
                collection.ChunkOverlap
            );

            var chunkContents = generatedChunks.Select(chunk => chunk.Content).ToArray();

            var generatedEmbeddings = await embeddingGenerator.GenerateAsync(
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

            if (generatedChunks.Count == 0)
            {
                throw new InvalidOperationException("The document produced no searchable chunks.");
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken
            );

            var existingChunks = await dbContext
                .AiDocumentChunks.Where(chunk => chunk.DocumentId == documentId)
                .ToListAsync(cancellationToken);

            dbContext.AiDocumentChunks.RemoveRange(existingChunks);

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

                dbContext.AiDocumentChunks.Add(chunk);

                dbContext.AiDocumentChunks.Add(chunk);
            }

            document.MarkReady();

            await dbContext.SaveChangesAsync(cancellationToken);

            var savedChunk = await dbContext
                .AiDocumentChunks.AsNoTracking()
                .SingleAsync(
                    chunk => chunk.DocumentId == documentId && chunk.ChunkNumber == 1,
                    cancellationToken
                );

            await transaction.CommitAsync(cancellationToken);

            return Results.Ok(
                new ProcessDocumentResponse(
                    document.Id,
                    generatedChunks.Count,
                    document.Status.ToString()
                )
            );
        }
        catch (Exception exception)
        {
            dbContext.ChangeTracker.Clear();

            var failedDocument = await dbContext.AiDocuments.FirstOrDefaultAsync(
                item => item.Id == documentId,
                cancellationToken
            );

            if (failedDocument is not null)
            {
                failedDocument.MarkFailed(exception.Message);

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Results.Problem(
                title: "Document processing failed",
                detail: exception.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetDocumentChunksAsync(
        Guid collectionId,
        Guid documentId,
        FusionDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var documentExists = await dbContext
            .AiDocuments.AsNoTracking()
            .AnyAsync(
                item => item.Id == documentId && item.CollectionId == collectionId,
                cancellationToken
            );

        if (!documentExists)
        {
            return Results.NotFound(new ErrorResponse($"Document '{documentId}' was not found."));
        }

        var chunks = await dbContext
            .AiDocumentChunks.AsNoTracking()
            .Where(chunk => chunk.DocumentId == documentId)
            .OrderBy(chunk => chunk.ChunkNumber)
            .Select(chunk => new AiDocumentChunkResponse(
                chunk.Id,
                chunk.DocumentId,
                chunk.ChunkNumber,
                chunk.Content,
                chunk.ContentHash,
                chunk.StartOffset,
                chunk.EndOffset,
                chunk.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Results.Ok(chunks);
    }

    private static async Task<IResult> CreateDocumentAsync(
        Guid collectionId,
        CreateAiDocumentRequest request,
        FusionDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var collectionExists = await dbContext
            .AiCollections.AsNoTracking()
            .AnyAsync(collection => collection.Id == collectionId, cancellationToken);

        if (!collectionExists)
        {
            return Results.NotFound(
                new ErrorResponse($"Collection '{collectionId}' was not found.")
            );
        }

        try
        {
            var metadataJson =
                request.Metadata is null
                || request.Metadata.Value.ValueKind
                    is System.Text.Json.JsonValueKind.Null
                        or System.Text.Json.JsonValueKind.Undefined
                    ? "{}"
                    : request.Metadata.Value.GetRawText();

            var document = AiDocument.Create(
                collectionId,
                request.Title,
                request.Content,
                metadataJson
            );

            dbContext.AiDocuments.Add(document);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/api/collections/{collectionId}/documents/{document.Id}",
                AiDocumentResponse.FromEntity(document)
            );
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ErrorResponse(exception.Message));
        }
    }

    private static async Task<IResult> GetDocumentsAsync(
        Guid collectionId,
        FusionDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var collectionExists = await dbContext
            .AiCollections.AsNoTracking()
            .AnyAsync(collection => collection.Id == collectionId, cancellationToken);

        if (!collectionExists)
        {
            return Results.NotFound(
                new ErrorResponse($"Collection '{collectionId}' was not found.")
            );
        }

        var documents = await dbContext
            .AiDocuments.AsNoTracking()
            .Where(document => document.CollectionId == collectionId)
            .OrderByDescending(document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(documents.Select(AiDocumentResponse.FromEntity));
    }

    private static async Task<IResult> GetDocumentByIdAsync(
        Guid collectionId,
        Guid documentId,
        FusionDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var document = await dbContext
            .AiDocuments.AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.CollectionId == collectionId && item.Id == documentId,
                cancellationToken
            );

        if (document is null)
        {
            return Results.NotFound(new ErrorResponse($"Document '{documentId}' was not found."));
        }

        return Results.Ok(AiDocumentResponse.FromEntity(document));
    }
}
