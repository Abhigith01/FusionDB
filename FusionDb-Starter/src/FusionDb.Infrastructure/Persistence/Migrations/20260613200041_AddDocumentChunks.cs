using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FusionDb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_document_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chunk_number = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    start_offset = table.Column<int>(type: "integer", nullable: false),
                    end_offset = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_document_chunks", x => x.id);
                    table.CheckConstraint("ck_ai_document_chunks_number", "chunk_number > 0");
                    table.CheckConstraint("ck_ai_document_chunks_offsets", "start_offset >= 0 AND end_offset > start_offset");
                    table.ForeignKey(
                        name: "FK_ai_document_chunks_ai_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "ai_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_document_chunks_content_hash",
                table: "ai_document_chunks",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "IX_ai_document_chunks_document_id",
                table: "ai_document_chunks",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_document_chunks_document_id_chunk_number",
                table: "ai_document_chunks",
                columns: new[] { "document_id", "chunk_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_document_chunks");
        }
    }
}
