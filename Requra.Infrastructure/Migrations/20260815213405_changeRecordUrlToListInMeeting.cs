using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class changeRecordUrlToListInMeeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordingUrl",
                table: "meeting_sessions");


            migrationBuilder.AddColumn<string>(
                name: "RecordingUrls",
                table: "meeting_sessions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordingUrls",
                table: "meeting_sessions");



            migrationBuilder.AddColumn<string>(
                name: "RecordingUrl",
                table: "meeting_sessions",
                type: "text",
                nullable: true);
        }
    }
}
