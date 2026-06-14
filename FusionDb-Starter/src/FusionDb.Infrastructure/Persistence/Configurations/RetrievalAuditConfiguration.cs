using FusionDb.Domain.Collections;
using FusionDb.Domain.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FusionDb.Infrastructure.Persistence.Configurations;

public sealed class RetrievalAuditConfiguration : IEntityTypeConfiguration<RetrievalAudit>
{
    public void Configure(EntityTypeBuilder<RetrievalAudit> builder)
    {
        builder.ToTable("retrieval_audits");

        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(audit => audit.CollectionId).HasColumnName("collection_id").IsRequired();

        builder
            .Property(audit => audit.Operation)
            .HasColumnName("operation")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(audit => audit.QueryText).HasColumnName("query_text").IsRequired();

        builder
            .Property(audit => audit.MetadataFilterJson)
            .HasColumnName("metadata_filter")
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(audit => audit.MinimumSimilarity)
            .HasColumnName("minimum_similarity")
            .IsRequired();

        builder
            .Property(audit => audit.RequestedLimit)
            .HasColumnName("requested_limit")
            .IsRequired();

        builder.Property(audit => audit.ResultCount).HasColumnName("result_count").IsRequired();

        builder
            .Property(audit => audit.DurationMilliseconds)
            .HasColumnName("duration_ms")
            .IsRequired();

        builder
            .Property(audit => audit.GenerationModel)
            .HasColumnName("generation_model")
            .HasMaxLength(200);

        builder.Property(audit => audit.Answer).HasColumnName("answer");

        builder.Property(audit => audit.Grounded).HasColumnName("grounded");

        builder
            .Property(audit => audit.ResultsJson)
            .HasColumnName("results")
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(audit => audit.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(audit => audit.ErrorMessage).HasColumnName("error_message");

        builder
            .Property(audit => audit.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .HasOne<AiCollection>()
            .WithMany()
            .HasForeignKey(audit => audit.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(audit => new { audit.CollectionId, audit.CreatedAt });

        builder.HasIndex(audit => new { audit.Operation, audit.CreatedAt });

        builder.HasIndex(audit => audit.Status);
    }
}
