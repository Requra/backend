using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    public partial class MakeStatusFieldInvitationStatusEnuminProjectReviewInvitationEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProjectReviewInvitations");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ProjectReviewInvitations",
                type: "integer",
                nullable: false,
                defaultValue: 0); 
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProjectReviewInvitations");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ProjectReviewInvitations",
                type: "text",
                nullable: false,
                defaultValue: "PENDING");
        }
    }
}