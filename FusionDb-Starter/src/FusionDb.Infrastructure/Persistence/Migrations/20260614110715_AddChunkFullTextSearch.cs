using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace FusionDb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "ai_document_chunks",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "content" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_document_chunks_search_vector",
                table: "ai_document_chunks",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ai_document_chunks_search_vector",
                table: "ai_document_chunks");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "ai_document_chunks");
        }
    }
}
