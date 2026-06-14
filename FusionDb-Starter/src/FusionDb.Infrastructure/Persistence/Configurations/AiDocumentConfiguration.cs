using FusionDb.Domain.Collections;
using FusionDb.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FusionDb.Infrastructure.Persistence.Configurations;

public sealed class AiDocumentConfiguration
    : IEntityTypeConfiguration<AiDocument>
{
    public void Configure(
        EntityTypeBuilder<AiDocument> builder)
    {
        builder.ToTable("ai_documents");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(document => document.CollectionId)
            .HasColumnName("collection_id")
            .IsRequired();

        builder.Property(document => document.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(document => document.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(document => document.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(document => document.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(document => document.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(document => document.FailureReason)
            .HasColumnName("failure_reason");

        builder.Property(document => document.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(document => document.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<AiCollection>()
            .WithMany()
            .HasForeignKey(document => document.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(document => document.CollectionId);

        builder.HasIndex(document => new
        {
            document.CollectionId,
            document.ContentHash
        });

        builder.HasIndex(document => document.Status);

        builder.HasIndex(document => document.MetadataJson)
            .HasMethod("gin");
    }
}