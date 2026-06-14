using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FusionDb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRetrievalAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "retrieval_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    query_text = table.Column<string>(type: "text", nullable: false),
                    metadata_filter = table.Column<string>(type: "jsonb", nullable: false),
                    minimum_similarity = table.Column<double>(type: "double precision", nullable: false),
                    requested_limit = table.Column<int>(type: "integer", nullable: false),
                    result_count = table.Column<int>(type: "integer", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    generation_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    answer = table.Column<string>(type: "text", nullable: true),
                    grounded = table.Column<bool>(type: "boolean", nullable: true),
                    results = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retrieval_audits", x => x.id);
                    table.ForeignKey(
                        name: "FK_retrieval_audits_ai_collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "ai_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_retrieval_audits_collection_id_created_at",
                table: "retrieval_audits",
                columns: new[] { "collection_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_retrieval_audits_operation_created_at",
                table: "retrieval_audits",
                columns: new[] { "operation", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_retrieval_audits_status",
                table: "retrieval_audits",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "retrieval_audits");
        }
    }
}
