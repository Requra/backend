using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInRequrament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Actor",
                table: "requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                table: "requirements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeduplicationKey",
                table: "requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityIssues",
                table: "requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityScore",
                table: "requirements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityWarnings",
                table: "requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewFeedback",
                table: "requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewdById",
                table: "requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "requirements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedById",
                table: "requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRequirementId",
                table: "requirements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RequirementSourceReference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Page = table.Column<int>(type: "integer", nullable: true),
                    Quote = table.Column<string>(type: "text", nullable: true),
                    ChunkId = table.Column<string>(type: "text", nullable: true),
                    SourceId = table.Column<string>(type: "text", nullable: true),
                    SourceType = table.Column<string>(type: "text", nullable: true),
                    DocumentName = table.Column<string>(type: "text", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementSourceReference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementSourceReference_requirements_RequirementId",
                        column: x => x.RequirementId,
                        principalTable: "requirements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_requirements_ReviewdById",
                table: "requirements",
                column: "ReviewdById");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementSourceReference_RequirementId",
                table: "RequirementSourceReference",
                column: "RequirementId");

            migrationBuilder.AddForeignKey(
                name: "FK_requirements_AspNetUsers_ReviewdById",
                table: "requirements",
                column: "ReviewdById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_requirements_AspNetUsers_ReviewdById",
                table: "requirements");

            migrationBuilder.DropTable(
                name: "RequirementSourceReference");

            migrationBuilder.DropIndex(
                name: "IX_requirements_ReviewdById",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "Actor",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "DeduplicationKey",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "QualityIssues",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "QualityWarnings",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "ReviewFeedback",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "ReviewdById",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "SourceRequirementId",
                table: "requirements");
        }
    }
}
