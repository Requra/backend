using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectToSummaryandRequirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "summaries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "requirements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_summaries_ProjectId",
                table: "summaries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_requirements_ProjectId",
                table: "requirements",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_requirements_projects_ProjectId",
                table: "requirements",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_summaries_projects_ProjectId",
                table: "summaries",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_requirements_projects_ProjectId",
                table: "requirements");

            migrationBuilder.DropForeignKey(
                name: "FK_summaries_projects_ProjectId",
                table: "summaries");

            migrationBuilder.DropIndex(
                name: "IX_summaries_ProjectId",
                table: "summaries");

            migrationBuilder.DropIndex(
                name: "IX_requirements_ProjectId",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "summaries");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "requirements");
        }
    }
}
