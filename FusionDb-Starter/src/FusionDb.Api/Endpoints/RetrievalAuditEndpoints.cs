using System.Text.Json;
using FusionDb.Api.Contracts;
using FusionDb.Api.Contracts.Observability;
using FusionDb.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FusionDb.Api.Endpoints;

public static class RetrievalAuditEndpoints
{
    public static IEndpointRouteBuilder MapRetrievalAuditEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/collections/{collectionId:guid}/retrieval-audits")
            .WithTags("Retrieval Observability");

        group.MapGet("/", GetAuditsAsync);

        group.MapGet("/{auditId:guid}", GetAuditByIdAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAuditsAsync(
        Guid collectionId,
        [AsParameters] RetrievalAuditQuery request,
        FusionDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (request.Page < 1)
        {
            return Results.BadRequest(new ErrorResponse("Page must be greater than zero."));
        }

        if (request.PageSize is < 1 or > 100)
        {
            return Results.BadRequest(new ErrorResponse("Page size must be between 1 and 100."));
        }

        var collectionExists = await dbContext
            .AiCollections.AsNoTracking()
            .AnyAsync(collection => collection.Id == collectionId, cancellationToken);

        if (!collectionExists)
        {
            return Results.NotFound(
                new ErrorResponse($"Collection '{collectionId}' was not found.")
            );
        }

        var query = dbContext
            .RetrievalAudits.AsNoTracking()
            .Where(audit => audit.CollectionId == collectionId);

        if (!string.IsNullOrWhiteSpace(request.Operation))
        {
            var operation = request.Operation.Trim().ToLowerInvariant();

            query = query.Where(audit => audit.Operation == operation);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();

            query = query.Where(audit => audit.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(audit => audit.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(audit => new RetrievalAuditSummaryResponse(
                audit.Id,
                audit.CollectionId,
                audit.Operation,
                audit.QueryText,
                audit.ResultCount,
                audit.DurationMilliseconds,
                audit.GenerationModel,
                audit.Grounded,
                audit.Status,
                audit.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Results.Ok(
            new PagedRetrievalAuditsResponse(request.Page, request.PageSize, totalCount, items)
        );
    }

    private static async Task<IResult> GetAuditByIdAsync(
        Guid collectionId,
        Guid auditId,
        FusionDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var audit = await dbContext
            .RetrievalAudits.AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == auditId && item.CollectionId == collectionId,
                cancellationToken
            );

        if (audit is null)
        {
            return Results.NotFound(
                new ErrorResponse($"Retrieval audit '{auditId}' was not found.")
            );
        }

        return Results.Ok(
            new RetrievalAuditDetailResponse(
                audit.Id,
                audit.CollectionId,
                audit.Operation,
                audit.QueryText,
                ParseJson(audit.MetadataFilterJson, "{}"),
                audit.MinimumSimilarity,
                audit.RequestedLimit,
                audit.ResultCount,
                audit.DurationMilliseconds,
                audit.GenerationModel,
                audit.Answer,
                audit.Grounded,
                ParseJson(audit.ResultsJson, "[]"),
                audit.Status,
                audit.ErrorMessage,
                audit.CreatedAt
            )
        );
    }

    private static JsonElement ParseJson(string? json, string fallback)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(
                string.IsNullOrWhiteSpace(json) ? fallback : json
            );
        }
        catch (JsonException)
        {
            return JsonSerializer.Deserialize<JsonElement>(fallback);
        }
    }
}
