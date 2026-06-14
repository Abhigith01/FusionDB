using FusionDb.Domain.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FusionDb.Infrastructure.Persistence.Configurations;

public sealed class AiCollectionConfiguration
    : IEntityTypeConfiguration<AiCollection>
{
    public void Configure(
        EntityTypeBuilder<AiCollection> builder)
    {
        builder.ToTable(
            "ai_collections",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_ai_collections_vector_dimensions",
                    "vector_dimensions > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_ai_collections_chunk_size",
                    "chunk_size > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_ai_collections_chunk_overlap",
                    "chunk_overlap >= 0 AND chunk_overlap < chunk_size");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.Property(x => x.VectorDimensions)
            .HasColumnName("vector_dimensions")
            .IsRequired();

        builder.Property(x => x.EmbeddingModel)
            .HasColumnName("embedding_model")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ChunkSize)
            .HasColumnName("chunk_size")
            .IsRequired();

        builder.Property(x => x.ChunkOverlap)
            .HasColumnName("chunk_overlap")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}