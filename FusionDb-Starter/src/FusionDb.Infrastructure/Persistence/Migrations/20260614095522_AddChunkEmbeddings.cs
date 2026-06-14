using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace FusionDb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "embedded_at",
                table: "ai_document_chunks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "embedding",
                table: "ai_document_chunks",
                type: "vector",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "embedding_model",
                table: "ai_document_chunks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "embedded_at",
                table: "ai_document_chunks");

            migrationBuilder.DropColumn(
                name: "embedding",
                table: "ai_document_chunks");

            migrationBuilder.DropColumn(
                name: "embedding_model",
                table: "ai_document_chunks");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
