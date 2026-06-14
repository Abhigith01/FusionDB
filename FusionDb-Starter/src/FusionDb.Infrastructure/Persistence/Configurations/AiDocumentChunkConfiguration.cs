using FusionDb.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FusionDb.Infrastructure.Persistence.Configurations;

public sealed class AiDocumentChunkConfiguration : IEntityTypeConfiguration<AiDocumentChunk>
{
    public void Configure(EntityTypeBuilder<AiDocumentChunk> builder)
    {
        builder.ToTable(
            "ai_document_chunks",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_ai_document_chunks_number", "chunk_number > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_ai_document_chunks_offsets",
                    "start_offset >= 0 AND end_offset > start_offset"
                );
            }
        );

        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(chunk => chunk.DocumentId).HasColumnName("document_id").IsRequired();

        builder.Property(chunk => chunk.ChunkNumber).HasColumnName("chunk_number").IsRequired();

        builder.Property(chunk => chunk.Content).HasColumnName("content").IsRequired();

        builder
            .Property(chunk => chunk.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(chunk => chunk.StartOffset).HasColumnName("start_offset").IsRequired();

        builder.Property(chunk => chunk.EndOffset).HasColumnName("end_offset").IsRequired();

        builder
            .Property(chunk => chunk.Embedding)
            .HasColumnName("embedding")
            .HasColumnType("vector");

        builder.Property(chunk => chunk.SearchVector).HasColumnName("search_vector");

        builder.HasGeneratedTsVectorColumn(
            chunk => chunk.SearchVector,
            "english",
            chunk => new { chunk.Content }
        );

        builder.HasIndex(chunk => chunk.SearchVector).HasMethod("GIN");

        builder
            .Property(chunk => chunk.EmbeddingModel)
            .HasColumnName("embedding_model")
            .HasMaxLength(200);

        builder
            .Property(chunk => chunk.EmbeddedAt)
            .HasColumnName("embedded_at")
            .HasColumnType("timestamp with time zone");

        builder
            .Property(chunk => chunk.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .HasOne<AiDocument>()
            .WithMany()
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(chunk => chunk.DocumentId);

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.ChunkNumber }).IsUnique();

        builder.HasIndex(chunk => chunk.ContentHash);
    }
}
