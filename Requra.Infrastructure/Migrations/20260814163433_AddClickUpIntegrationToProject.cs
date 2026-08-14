using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClickUpIntegrationToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "projects",
                newName: "is_deleted");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                table: "projects",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<string>(
                name: "click_up_access_token",
                table: "projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "click_up_list_id",
                table: "projects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "click_up_space_id",
                table: "projects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "click_up_team_id",
                table: "projects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "click_up_token_expires_at",
                table: "projects",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_click_up_connected",
                table: "projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "click_up_access_token",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "click_up_list_id",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "click_up_space_id",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "click_up_team_id",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "click_up_token_expires_at",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "is_click_up_connected",
                table: "projects");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "projects",
                newName: "IsDeleted");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "projects",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
