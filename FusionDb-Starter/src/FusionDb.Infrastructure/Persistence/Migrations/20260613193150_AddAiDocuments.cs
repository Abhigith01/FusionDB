using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FusionDb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_documents_ai_collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "ai_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_documents_collection_id",
                table: "ai_documents",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_documents_collection_id_content_hash",
                table: "ai_documents",
                columns: new[] { "collection_id", "content_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_documents_metadata",
                table: "ai_documents",
                column: "metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ai_documents_status",
                table: "ai_documents",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_documents");
        }
    }
}
