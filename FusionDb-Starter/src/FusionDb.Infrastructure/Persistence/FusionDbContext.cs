using FusionDb.Domain.Collections;
using FusionDb.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace FusionDb.Infrastructure.Persistence;

public sealed class FusionDbContext : DbContext
{
    public FusionDbContext(DbContextOptions<FusionDbContext> options)
        : base(options) { }

    public DbSet<AiCollection> AiCollections => Set<AiCollection>();

    public DbSet<AiDocument> AiDocuments => Set<AiDocument>();

    public DbSet<AiDocumentChunk> AiDocumentChunks => Set<AiDocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FusionDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
