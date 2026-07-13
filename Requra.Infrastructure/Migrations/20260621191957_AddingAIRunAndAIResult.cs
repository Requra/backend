using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingAIRunAndAIResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analysis_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    progress = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    started_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "analysis_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    analysis_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_analysis_results_analysis_runs_analysis_run_id",
                        column: x => x.analysis_run_id,
                        principalTable: "analysis_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analysis_results_analysis_run_id",
                table: "analysis_results",
                column: "analysis_run_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_results");

            migrationBuilder.DropTable(
                name: "analysis_runs");
        }
    }
}
