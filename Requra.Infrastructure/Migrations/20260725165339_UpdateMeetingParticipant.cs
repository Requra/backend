using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMeetingParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_meeting_participants",
                table: "meeting_participants");

            migrationBuilder.RenameIndex(
                name: "IX_meeting_participants_meeting_id",
                table: "meeting_participants",
                newName: "ix_meeting_participants_meeting_id");

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "meeting_participants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "meeting_participants",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "meeting_participants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "consented_at",
                table: "meeting_participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "meeting_participants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "meeting_participants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "left_at",
                table: "meeting_participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "recording_consent",
                table: "meeting_participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "meeting_participants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            // --- Backfill existing rows before enforcing the new primary key / enum values 
            migrationBuilder.Sql("UPDATE meeting_participants SET id = gen_random_uuid();");
            migrationBuilder.Sql("UPDATE meeting_participants SET status = 'Joined' WHERE status = '';");

           
            //migrationBuilder.Sql(
            //    "UPDATE meeting_participants mp " +
            //    "SET display_name = u.full_name, email = u.\"Email\" " +
            //    "FROM users u " +
            //    "WHERE mp.user_id = u.\"Id\" AND mp.display_name IS NULL;");

            migrationBuilder.AddPrimaryKey(
                name: "PK_meeting_participants",
                table: "meeting_participants",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_meeting_participants_meeting_user",
                table: "meeting_participants",
                columns: new[] { "meeting_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_meeting_participants_user_id",
                table: "meeting_participants",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_meeting_participants",
                table: "meeting_participants");

            migrationBuilder.DropIndex(
                name: "ix_meeting_participants_meeting_user",
                table: "meeting_participants");

            migrationBuilder.DropIndex(
                name: "IX_meeting_participants_user_id",
                table: "meeting_participants");

            migrationBuilder.DropColumn(
                name: "id",
                table: "meeting_participants");

            migrationBuilder.DropColumn(
                name: "consented_at",
                table: "meeting_participants");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "meeting_participants");

            migrationBuilder.DropColumn(
                name: "email",
                table: "meeting_participants");

            migrationBuilder.DropColumn(
                name: "left_at",
                table: "meeting_participants");

            migrationBuilder.DropColumn(
                name: "recording_consent",
                table: "meeting_participants");

            migrationBuilder.DropColumn(
                name: "status",
                table: "meeting_participants");

            migrationBuilder.RenameIndex(
                name: "ix_meeting_participants_meeting_id",
                table: "meeting_participants",
                newName: "IX_meeting_participants_meeting_id");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "meeting_participants",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "meeting_participants",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddPrimaryKey(
                name: "PK_meeting_participants",
                table: "meeting_participants",
                columns: new[] { "user_id", "meeting_id" });
        }
    }
}