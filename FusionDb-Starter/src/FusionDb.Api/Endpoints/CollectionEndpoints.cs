using FusionDb.Api.Contracts;
using FusionDb.Api.Contracts.Collections;
using FusionDb.Domain.Collections;
using FusionDb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FusionDb.Api.Endpoints;

public static class CollectionEndpoints
{
    public static IEndpointRouteBuilder MapCollectionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/collections")
            .WithTags("AI Collections");

        group.MapPost("/", CreateCollectionAsync);

        group.MapGet("/", GetCollectionsAsync);

        group.MapGet("/{id:guid}", GetCollectionByIdAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateCollectionAsync(
        CreateAiCollectionRequest request,
        FusionDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedName = request.Name?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return Results.BadRequest(
                    new ErrorResponse(
                        "Collection name is required."));
            }

            var collectionExists =
                await dbContext.AiCollections
                    .AnyAsync(
                        collection =>
                            collection.Name == normalizedName,
                        cancellationToken);

            if (collectionExists)
            {
                return Results.Conflict(
                    new ErrorResponse(
                        $"Collection '{normalizedName}' already exists."));
            }

           var collection = AiCollection.Create(
            normalizedName,
            request.Description,
            request.VectorDimensions,
            request.EmbeddingModel,
            request.ChunkSize,
            request.ChunkOverlap);

            dbContext.AiCollections.Add(collection);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/api/collections/{collection.Id}",
                AiCollectionResponse.FromEntity(collection));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(
                new ErrorResponse(exception.Message));
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            return Results.Conflict(
                new ErrorResponse(
                    $"A collection named '{request.Name}' already exists."));
        }
    }

    private static async Task<IResult> GetCollectionsAsync(
        FusionDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var collections =
            await dbContext.AiCollections
                .AsNoTracking()
                .OrderBy(collection => collection.Name)
                .Select(collection => new AiCollectionResponse(
                    collection.Id,
                    collection.Name,
                    collection.Description,
                    collection.VectorDimensions,
                    collection.EmbeddingModel,
                    collection.ChunkSize,
                    collection.ChunkOverlap,
                    collection.CreatedAt,
                    collection.UpdatedAt))
                .ToListAsync(cancellationToken);

        return Results.Ok(collections);
    }

    private static async Task<IResult> GetCollectionByIdAsync(
        Guid id,
        FusionDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var collection =
            await dbContext.AiCollections
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    collection => collection.Id == id,
                    cancellationToken);

        if (collection is null)
        {
            return Results.NotFound(
                new ErrorResponse(
                    $"Collection '{id}' was not found."));
        }

        return Results.Ok(
            AiCollectionResponse.FromEntity(collection));
    }
}