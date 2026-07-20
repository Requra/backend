using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatestoProjectReviewInvitationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix StakeholderId (Guid -> string)
            migrationBuilder.AlterColumn<string>(
                name: "StakeholderId",
                table: "ProjectReviewInvitations",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.DropColumn(
                name: "Permission",
                table: "ProjectReviewInvitations");

            migrationBuilder.AddColumn<int>(
                name: "Permission",
                table: "ProjectReviewInvitations",
                type: "integer",
                nullable: false,
                defaultValue: 0); 
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove int column
            migrationBuilder.DropColumn(
                name: "Permission",
                table: "ProjectReviewInvitations");

            // Restore as string
            migrationBuilder.AddColumn<string>(
                name: "Permission",
                table: "ProjectReviewInvitations",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Restore StakeholderId
            migrationBuilder.AlterColumn<Guid>(
                name: "StakeholderId",
                table: "ProjectReviewInvitations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
