using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStoryContentEditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserStoryQuality_user_stories_UserStoryId",
                table: "UserStoryQuality");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserStoryQuality",
                table: "UserStoryQuality");

            migrationBuilder.RenameTable(
                name: "UserStoryQuality",
                newName: "user_story_qualities");

            migrationBuilder.RenameColumn(
                name: "Warnings",
                table: "user_story_qualities",
                newName: "warnings");

            migrationBuilder.RenameColumn(
                name: "Score",
                table: "user_story_qualities",
                newName: "score");

            migrationBuilder.RenameColumn(
                name: "Issues",
                table: "user_story_qualities",
                newName: "issues");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "user_story_qualities",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserStoryId",
                table: "user_story_qualities",
                newName: "user_story_id");

            migrationBuilder.RenameIndex(
                name: "IX_UserStoryQuality_UserStoryId",
                table: "user_story_qualities",
                newName: "IX_user_story_qualities_user_story_id");

            migrationBuilder.AddColumn<string>(
                name: "labels",
                table: "user_stories",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "last_modified_by",
                table: "user_stories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "revision_number",
                table: "user_stories",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "revision_source",
                table: "user_stories",
                type: "text",
                nullable: false,
                defaultValue: "AI_GENERATED");

            migrationBuilder.Sql(@"ALTER TABLE user_story_qualities ALTER COLUMN warnings TYPE jsonb USING to_jsonb(warnings);");
            migrationBuilder.Sql(@"ALTER TABLE user_story_qualities ALTER COLUMN issues TYPE jsonb USING to_jsonb(issues);");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "user_story_qualities",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "quality_status",
                table: "user_story_qualities",
                type: "text",
                nullable: false,
                defaultValue: "NOT_EVALUATED");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_story_qualities",
                table: "user_story_qualities",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_story_qualities_user_stories_user_story_id",
                table: "user_story_qualities",
                column: "user_story_id",
                principalTable: "user_stories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_story_qualities_user_stories_user_story_id",
                table: "user_story_qualities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_story_qualities",
                table: "user_story_qualities");

            migrationBuilder.DropColumn(
                name: "labels",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "revision_number",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "revision_source",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "quality_status",
                table: "user_story_qualities");

            migrationBuilder.RenameTable(
                name: "user_story_qualities",
                newName: "UserStoryQuality");

            migrationBuilder.RenameColumn(
                name: "warnings",
                table: "UserStoryQuality",
                newName: "Warnings");

            migrationBuilder.RenameColumn(
                name: "score",
                table: "UserStoryQuality",
                newName: "Score");

            migrationBuilder.RenameColumn(
                name: "issues",
                table: "UserStoryQuality",
                newName: "Issues");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "UserStoryQuality",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_story_id",
                table: "UserStoryQuality",
                newName: "UserStoryId");

            migrationBuilder.RenameIndex(
                name: "IX_user_story_qualities_user_story_id",
                table: "UserStoryQuality",
                newName: "IX_UserStoryQuality_UserStoryId");

            migrationBuilder.Sql(@"ALTER TABLE ""UserStoryQuality"" ALTER COLUMN ""Warnings"" TYPE text[] USING ARRAY(SELECT jsonb_array_elements_text(""Warnings""));");
            migrationBuilder.Sql(@"ALTER TABLE ""UserStoryQuality"" ALTER COLUMN ""Issues"" TYPE text[] USING ARRAY(SELECT jsonb_array_elements_text(""Issues""));");
            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "UserStoryQuality",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserStoryQuality",
                table: "UserStoryQuality",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserStoryQuality_user_stories_UserStoryId",
                table: "UserStoryQuality",
                column: "UserStoryId",
                principalTable: "user_stories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
