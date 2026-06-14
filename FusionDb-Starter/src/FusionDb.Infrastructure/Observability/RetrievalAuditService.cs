using FusionDb.Application.Observability;
using FusionDb.Domain.Observability;
using FusionDb.Infrastructure.Persistence;

namespace FusionDb.Infrastructure.Observability;

public sealed class RetrievalAuditService : IRetrievalAuditService
{
    private readonly FusionDbContext _dbContext;

    public RetrievalAuditService(FusionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> RecordAsync(
        RetrievalAuditInput input,
        CancellationToken cancellationToken = default
    )
    {
        var audit = RetrievalAudit.Create(
            input.CollectionId,
            input.Operation,
            input.QueryText,
            input.MetadataFilterJson,
            input.MinimumSimilarity,
            input.RequestedLimit,
            input.ResultCount,
            input.DurationMilliseconds,
            input.GenerationModel,
            input.Answer,
            input.Grounded,
            input.ResultsJson,
            input.Status,
            input.ErrorMessage
        );

        _dbContext.RetrievalAudits.Add(audit);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return audit.Id;
    }
}
