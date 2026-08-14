using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addversionandquailtystatustorequirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentId",
                table: "RequirementSourceReference",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedById",
                table: "requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityStatus",
                table: "requirements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "requirements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RequirementSourceReference_DocumentId",
                table: "RequirementSourceReference",
                column: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequirementSourceReference_documents_DocumentId",
                table: "RequirementSourceReference",
                column: "DocumentId",
                principalTable: "documents",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequirementSourceReference_documents_DocumentId",
                table: "RequirementSourceReference");

            migrationBuilder.DropIndex(
                name: "IX_RequirementSourceReference_DocumentId",
                table: "RequirementSourceReference");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "RequirementSourceReference");

            migrationBuilder.DropColumn(
                name: "LastModifiedById",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "version",
                table: "requirements");

            
        }
    }
}
