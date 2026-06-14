using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FusionDb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_collections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    vector_dimensions = table.Column<int>(type: "integer", nullable: false),
                    embedding_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    chunk_size = table.Column<int>(type: "integer", nullable: false),
                    chunk_overlap = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_collections", x => x.id);
                    table.CheckConstraint("ck_ai_collections_chunk_overlap", "chunk_overlap >= 0 AND chunk_overlap < chunk_size");
                    table.CheckConstraint("ck_ai_collections_chunk_size", "chunk_size > 0");
                    table.CheckConstraint("ck_ai_collections_vector_dimensions", "vector_dimensions > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_collections_name",
                table: "ai_collections",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_collections");
        }
    }
}
