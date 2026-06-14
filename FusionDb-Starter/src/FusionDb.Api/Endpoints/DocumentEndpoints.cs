using System.Text;
using System.Text.Json;
using FusionDb.Api.Contracts;
using FusionDb.Api.Contracts.Documents;
using FusionDb.Application.Documents;
using FusionDb.Domain.Documents;
using FusionDb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FusionDb.Api.Endpoints;

public static class DocumentEndpoints
{
    private const long MaximumUploadSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedFileExtensions = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ".txt",
        ".md",
        ".markdown",
        ".pdf",
    };

    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/collections/{collectionId:guid}/documents")
            .WithTags("AI Documents");

        group.MapPost("/", CreateDocumentAsync);
        group.MapPost("/upload", UploadDocumentAsync).DisableAntiforgery();
        group.MapGet("/", GetDocumentsAsync);
        group.MapGet("/{documentId:guid}", GetDocumentByIdAsync);

        group.MapPost("/{documentId:guid}/process", ProcessDocumentAsync);

        group.MapGet("/{documentId:guid}/chunks", GetDocumentChunksAsync);

        return endpoints;
    }

    private static async Task<IResult> UploadDocumentAsync(
        Guid collectionId,
        IFormFile file,
        FusionDbContext dbContext,
        IDocumentProcessingService processingService,
        IPdfTextExtractor pdfTextExtractor,
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

        if (file.Length == 0)
        {
            return Results.BadRequest(new ErrorResponse("The uploaded file is empty."));
        }

        if (file.Length > MaximumUploadSize)
        {
            return Results.BadRequest(new ErrorResponse("The uploaded file cannot exceed 5 MB."));
        }

        var safeFileName = Path.GetFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return Results.BadRequest(new ErrorResponse("The uploaded file name is invalid."));
        }

        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();

        if (!AllowedFileExtensions.Contains(extension))
        {
            return Results.BadRequest(
                new ErrorResponse("Only .txt, .md, .markdown, and .pdf files are supported.")
            );
        }

        string content;
        int? pageCount = null;

        try
        {
            await using var fileStream = file.OpenReadStream();

            if (extension == ".pdf")
            {
                var extractionResult = await pdfTextExtractor.ExtractAsync(
                    fileStream,
                    cancellationToken
                );

                content = extractionResult.Text;
                pageCount = extractionResult.PageCount;
            }
            else
            {
                using var reader = new StreamReader(
                    fileStream,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true
                );

                content = await reader.ReadToEndAsync(cancellationToken);

                if (content.Contains('\0'))
                {
                    return Results.BadRequest(
                        new ErrorResponse("The uploaded file does not appear to be a text file.")
                    );
                }
            }
        }
        catch (Exception exception)
        {
            return Results.BadRequest(
                new ErrorResponse($"The file could not be read: {exception.Message}")
            );
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            var message =
                extension == ".pdf"
                    ? "The PDF contains no extractable text. " + "It may be scanned or image-only."
                    : "The uploaded file contains no readable text.";

            return Results.BadRequest(new ErrorResponse(message));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Results.BadRequest(
                new ErrorResponse("The uploaded file contains no readable text.")
            );
        }

        if (content.Contains('\0'))
        {
            return Results.BadRequest(
                new ErrorResponse("The uploaded file does not appear to be a text file.")
            );
        }

        var title = Path.GetFileNameWithoutExtension(safeFileName);

        var metadataJson = JsonSerializer.Serialize(
            new
            {
                source = "file-upload",
                fileName = safeFileName,
                extension,
                contentType = string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType,
                sizeBytes = file.Length,
                pageCount,
                uploadedAt = DateTimeOffset.UtcNow,
            }
        );

        var document = AiDocument.Create(collectionId, title, content, metadataJson);

        dbContext.AiDocuments.Add(document);

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var processingResult = await processingService.ProcessAsync(
                collectionId,
                document.Id,
                cancellationToken
            );

            return Results.Created(
                $"/api/collections/{collectionId}" + $"/documents/{document.Id}",
                new UploadDocumentResponse(
                    document.Id,
                    document.Title,
                    safeFileName,
                    file.Length,
                    pageCount,
                    processingResult.ChunkCount,
                    processingResult.Status
                )
            );
        }
        catch (Exception exception)
        {
            return Results.Problem(
                title: "File ingestion failed",
                detail: $"Document '{document.Id}' was created, "
                    + $"but processing failed: "
                    + $"{exception.Message}",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> ProcessDocumentAsync(
        Guid collectionId,
        Guid documentId,
        IDocumentProcessingService processingService,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await processingService.ProcessAsync(
                collectionId,
                documentId,
                cancellationToken
            );

            if (!result.CollectionFound)
            {
                return Results.NotFound(
                    new ErrorResponse($"Collection '{collectionId}' was not found.")
                );
            }

            if (!result.DocumentFound)
            {
                return Results.NotFound(
                    new ErrorResponse($"Document '{documentId}' was not found.")
                );
            }

            return Results.Ok(
                new ProcessDocumentResponse(result.DocumentId, result.ChunkCount, result.Status)
            );
        }
        catch (Exception exception)
        {
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
