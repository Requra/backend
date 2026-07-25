using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteTokenToInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteToken",
                table: "invitations",
                type: "text",
                nullable: false,
                defaultValue: "");
            // solve the problem of existing invitations having empty InviteToken values by generating a unique token for each one
            migrationBuilder.Sql("UPDATE invitations SET \"InviteToken\" = 'legacy_' || id::text WHERE \"InviteToken\" = '';");

            migrationBuilder.CreateIndex(
                name: "ix_meeting_invitations_invite_token",
                table: "invitations",
                column: "InviteToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_meeting_invitations_invite_token",
                table: "invitations");

            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "invitations");
        }
    }
}
