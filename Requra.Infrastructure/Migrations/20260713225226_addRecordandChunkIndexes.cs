using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addRecordandChunkIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_recordings_meeting_id",
                table: "recordings");

            migrationBuilder.RenameIndex(
                name: "IX_recording_chunks_recording_id_chunk_number",
                table: "recording_chunks",
                newName: "ux_recording_chunks_recording_id_chunk_number");

            migrationBuilder.CreateIndex(
                name: "ux_recordings_meeting_id_one_active",
                table: "recordings",
                column: "meeting_id",
                unique: true,
                filter: "\"status\" IN ('Started', 'Uploading', 'Ending')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_recordings_meeting_id_one_active",
                table: "recordings");

            migrationBuilder.RenameIndex(
                name: "ux_recording_chunks_recording_id_chunk_number",
                table: "recording_chunks",
                newName: "IX_recording_chunks_recording_id_chunk_number");

            migrationBuilder.CreateIndex(
                name: "IX_recordings_meeting_id",
                table: "recordings",
                column: "meeting_id");
        }
    }
}
