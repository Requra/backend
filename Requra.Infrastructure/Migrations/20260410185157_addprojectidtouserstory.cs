using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addprojectidtouserstory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "user_stories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ProjectType",
                table: "projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "comments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_user_stories_ProjectId",
                table: "user_stories",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_user_stories_projects_ProjectId",
                table: "user_stories",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_stories_projects_ProjectId",
                table: "user_stories");

            migrationBuilder.DropIndex(
                name: "IX_user_stories_ProjectId",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "ProjectType",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "comments");
        }
    }
}
