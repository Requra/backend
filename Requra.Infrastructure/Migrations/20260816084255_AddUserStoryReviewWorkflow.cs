using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStoryReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "review_feedback",
                table: "user_stories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "user_stories",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewed_by_id",
                table: "user_stories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "user_stories",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "review_feedback",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "reviewed_by_id",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "version",
                table: "user_stories");
        }
    }
}
