using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addingRecordingandmeetingupdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meeting_participants_meeting_sessions_meeting_id",
                table: "meeting_participants");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "meeting_sessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "session_token",
                table: "meeting_sessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "platform_url",
                table: "meeting_sessions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "meeting_sessions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "RecordingUrl",
                table: "meeting_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranscriptDocumentUrl",
                table: "meeting_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "meeting_sessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "meeting_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duration_minutes",
                table: "meeting_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "host_id",
                table: "meeting_sessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "project_id",
                table: "meeting_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_at",
                table: "meeting_sessions",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "meeting_sessions",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transcript_status",
                table: "meeting_sessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "meeting_sessions",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recordings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    meeting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_id = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    storage_url = table.Column<string>(type: "text", nullable: true),
                    public_id = table.Column<string>(type: "text", nullable: true),
                    total_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_chunks = table.Column<int>(type: "integer", nullable: false),
                    expected_chunks = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recordings", x => x.id);
                    table.ForeignKey(
                        name: "FK_recordings_AspNetUsers_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recordings_meeting_sessions_meeting_id",
                        column: x => x.meeting_id,
                        principalTable: "meeting_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recording_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recording_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chunk_number = table.Column<int>(type: "integer", nullable: false),
                    storage_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    public_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recording_chunks", x => x.id);
                    table.ForeignKey(
                        name: "FK_recording_chunks_recordings_recording_id",
                        column: x => x.recording_id,
                        principalTable: "recordings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_meeting_sessions_created_by_id",
                table: "meeting_sessions",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_sessions_host_id",
                table: "meeting_sessions",
                column: "host_id");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_sessions_project_id",
                table: "meeting_sessions",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_sessions_scheduled_at",
                table: "meeting_sessions",
                column: "scheduled_at");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_sessions_status",
                table: "meeting_sessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_recording_chunks_recording_id",
                table: "recording_chunks",
                column: "recording_id");

            migrationBuilder.CreateIndex(
                name: "IX_recording_chunks_recording_id_chunk_number",
                table: "recording_chunks",
                columns: new[] { "recording_id", "chunk_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recordings_created_by_id",
                table: "recordings",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_recordings_meeting_id",
                table: "recordings",
                column: "meeting_id");

            migrationBuilder.CreateIndex(
                name: "IX_recordings_status",
                table: "recordings",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "FK_meeting_participants_meeting_sessions_meeting_id",
                table: "meeting_participants",
                column: "meeting_id",
                principalTable: "meeting_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_meeting_sessions_AspNetUsers_created_by_id",
                table: "meeting_sessions",
                column: "created_by_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_meeting_sessions_AspNetUsers_host_id",
                table: "meeting_sessions",
                column: "host_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_meeting_sessions_projects_project_id",
                table: "meeting_sessions",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meeting_participants_meeting_sessions_meeting_id",
                table: "meeting_participants");

            migrationBuilder.DropForeignKey(
                name: "FK_meeting_sessions_AspNetUsers_created_by_id",
                table: "meeting_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_meeting_sessions_AspNetUsers_host_id",
                table: "meeting_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_meeting_sessions_projects_project_id",
                table: "meeting_sessions");

            migrationBuilder.DropTable(
                name: "recording_chunks");

            migrationBuilder.DropTable(
                name: "recordings");

            migrationBuilder.DropIndex(
                name: "IX_meeting_sessions_created_by_id",
                table: "meeting_sessions");

            migrationBuilder.DropIndex(
                name: "IX_meeting_sessions_host_id",
                table: "meeting_sessions");

            migrationBuilder.DropIndex(
                name: "IX_meeting_sessions_project_id",
                table: "meeting_sessions");

            migrationBuilder.DropIndex(
                name: "IX_meeting_sessions_scheduled_at",
                table: "meeting_sessions");

            migrationBuilder.DropIndex(
                name: "IX_meeting_sessions_status",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "RecordingUrl",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "TranscriptDocumentUrl",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "description",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "duration_minutes",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "host_id",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "title",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "transcript_status",
                table: "meeting_sessions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "meeting_sessions");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "meeting_sessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "session_token",
                table: "meeting_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "platform_url",
                table: "meeting_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "meeting_sessions",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_meeting_participants_meeting_sessions_meeting_id",
                table: "meeting_participants",
                column: "meeting_id",
                principalTable: "meeting_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
