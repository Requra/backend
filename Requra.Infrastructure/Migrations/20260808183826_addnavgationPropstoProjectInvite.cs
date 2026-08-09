using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addnavgationPropstoProjectInvite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old ProjectId column
            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ProjectReviewInvitations");

            // Add ProjectId again with UUID type
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "ProjectReviewInvitations",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviewInvitations_InvitedById",
                table: "ProjectReviewInvitations",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviewInvitations_ProjectId",
                table: "ProjectReviewInvitations",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectReviewInvitations_AspNetUsers_InvitedById",
                table: "ProjectReviewInvitations",
                column: "InvitedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectReviewInvitations_projects_ProjectId",
                table: "ProjectReviewInvitations",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectReviewInvitations_AspNetUsers_InvitedById",
                table: "ProjectReviewInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectReviewInvitations_projects_ProjectId",
                table: "ProjectReviewInvitations");

            migrationBuilder.DropIndex(
                name: "IX_ProjectReviewInvitations_InvitedById",
                table: "ProjectReviewInvitations");

            migrationBuilder.DropIndex(
                name: "IX_ProjectReviewInvitations_ProjectId",
                table: "ProjectReviewInvitations");

            // Drop UUID ProjectId
            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ProjectReviewInvitations");

            // Recreate it as text
            migrationBuilder.AddColumn<string>(
                name: "ProjectId",
                table: "ProjectReviewInvitations",
                type: "text",
                nullable: false);
        }
    }
}
