using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatesInRecordingEntityandRecordingChunk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "total_size_bytes",
                table: "recordings",
                newName: "received_bytes");

            migrationBuilder.AlterColumn<string>(
                name: "storage_url",
                table: "recordings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "public_id",
                table: "recordings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "expected_chunks",
                table: "recordings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "abandoned_at",
                table: "recordings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "content_type",
                table: "recordings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "recordings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "final_file_size_bytes",
                table: "recordings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "finalization_error",
                table: "recordings",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_chunk_received_at",
                table: "recordings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_extension",
                table: "recordings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "stopped_at",
                table: "recordings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage_key",
                table: "recordings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "upload_mode",
                table: "recordings",
                type: "text",
                nullable: false,
                defaultValue: "");

            //migrationBuilder.AddColumn<uint>(
            //    name: "xmin",
            //    table: "recordings",
            //    type: "xid",
            //    rowVersion: true,
            //    nullable: false,
            //    defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "checksum",
                table: "recording_chunks",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "content_type",
                table: "recording_chunks",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "recording_chunks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "recording_chunks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "recording_chunks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "storage_key",
                table: "recording_chunks",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "upload_attempt_count",
                table: "recording_chunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            //migrationBuilder.AddColumn<uint>(
            //    name: "xmin",
            //    table: "recording_chunks",
            //    type: "xid",
            //    rowVersion: true,
            //    nullable: false,
            //    defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_recordings_meeting_id_status",
                table: "recordings",
                columns: new[] { "meeting_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_recording_chunks_status",
                table: "recording_chunks",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_recordings_meeting_id_status",
                table: "recordings");

            migrationBuilder.DropIndex(
                name: "IX_recording_chunks_status",
                table: "recording_chunks");

            migrationBuilder.DropColumn(
                name: "abandoned_at",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "content_type",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "final_file_size_bytes",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "finalization_error",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "last_chunk_received_at",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "original_extension",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "stopped_at",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "storage_key",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "upload_mode",
                table: "recordings");

            //migrationBuilder.DropColumn(
            //    name: "xmin",
            //    table: "recordings");

            migrationBuilder.DropColumn(
                name: "checksum",
                table: "recording_chunks");

            migrationBuilder.DropColumn(
                name: "content_type",
                table: "recording_chunks");

            migrationBuilder.DropColumn(
                name: "error_message",
                table: "recording_chunks");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "recording_chunks");

            migrationBuilder.DropColumn(
                name: "status",
                table: "recording_chunks");

            migrationBuilder.DropColumn(
                name: "storage_key",
                table: "recording_chunks");

            migrationBuilder.DropColumn(
                name: "upload_attempt_count",
                table: "recording_chunks");

            //migrationBuilder.DropColumn(
            //    name: "xmin",
            //    table: "recording_chunks");

            migrationBuilder.RenameColumn(
                name: "received_bytes",
                table: "recordings",
                newName: "total_size_bytes");

            migrationBuilder.AlterColumn<string>(
                name: "storage_url",
                table: "recordings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "public_id",
                table: "recordings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "expected_chunks",
                table: "recordings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
