using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInCommenttoSupportFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_user_stories_user_story_id",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_user_story_id",
                table: "comments");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "comments",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "user_story_id",
                table: "comments",
                newName: "target_id");

            migrationBuilder.RenameIndex(
                name: "IX_comments_author_id",
                table: "comments",
                newName: "ix_comments_author_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "comments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "comments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "comments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "author_id",
                table: "comments",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "comments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "comments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "comments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_read",
                table: "comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "project_id",
                table: "comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "resolution_note",
                table: "comments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "resolved_at",
                table: "comments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolved_by_id",
                table: "comments",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_title",
                table: "comments",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_type",
                table: "comments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_comments_ApplicationUserId",
                table: "comments",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "ix_comments_project_id",
                table: "comments",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_target_type_target_id",
                table: "comments",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_comments_AspNetUsers_ApplicationUserId",
                table: "comments",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_AspNetUsers_ApplicationUserId",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_ApplicationUserId",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "ix_comments_project_id",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "ix_comments_target_type_target_id",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "is_read",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "resolution_note",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "resolved_at",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "resolved_by_id",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "target_title",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "target_type",
                table: "comments");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "comments",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "target_id",
                table: "comments",
                newName: "user_story_id");

            migrationBuilder.RenameIndex(
                name: "ix_comments_author_id",
                table: "comments",
                newName: "IX_comments_author_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "comments",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "comments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "comments",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "comments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "author_id",
                table: "comments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "comments",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_comments_user_story_id",
                table: "comments",
                column: "user_story_id");

            migrationBuilder.AddForeignKey(
                name: "FK_comments_user_stories_user_story_id",
                table: "comments",
                column: "user_story_id",
                principalTable: "user_stories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
