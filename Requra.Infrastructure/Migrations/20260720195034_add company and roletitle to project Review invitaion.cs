using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addcompanyandroletitletoprojectReviewinvitaion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "ProjectReviewInvitations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleTitle",
                table: "ProjectReviewInvitations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Company",
                table: "ProjectReviewInvitations");

            migrationBuilder.DropColumn(
                name: "RoleTitle",
                table: "ProjectReviewInvitations");
        }
    }
}
