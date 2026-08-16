using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateUserStoryEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, convert text[] to jsonb by converting array to JSON array format
            migrationBuilder.Sql(
                @"ALTER TABLE user_stories 
                  ALTER COLUMN acceptance_criteria TYPE jsonb USING 
                  CASE 
                    WHEN acceptance_criteria IS NULL THEN '[]'::jsonb
                    WHEN array_length(acceptance_criteria, 1) IS NULL THEN '[]'::jsonb
                    ELSE to_jsonb(acceptance_criteria)
                  END");

            migrationBuilder.AddColumn<string>(
                name: "DeduplicationKey",
                table: "user_stories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRequirementId",
                table: "user_stories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoryPoints",
                table: "user_stories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "user_stories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "JiraFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Labels = table.Column<List<string>>(type: "text[]", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<string>(type: "text", nullable: true),
                    EpicName = table.Column<string>(type: "text", nullable: true),
                    Components = table.Column<List<string>>(type: "text[]", nullable: false),
                    IssueType = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StoryPoints = table.Column<int>(type: "integer", nullable: true),
                    UserStoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JiraFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JiraFields_user_stories_UserStoryId",
                        column: x => x.UserStoryId,
                        principalTable: "user_stories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_story_source_refs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    page = table.Column<string>(type: "text", nullable: true),
                    quote = table.Column<string>(type: "text", nullable: true),
                    chunk_id = table.Column<string>(type: "text", nullable: true),
                    source_id = table.Column<string>(type: "text", nullable: true),
                    source_type = table.Column<string>(type: "text", nullable: true),
                    document_name = table.Column<string>(type: "text", nullable: true),
                    confidence_score = table.Column<double>(type: "double precision", nullable: false),
                    user_story_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_story_source_refs", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_story_source_refs_user_stories_user_story_id",
                        column: x => x.user_story_id,
                        principalTable: "user_stories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStoryQuality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Issues = table.Column<List<string>>(type: "text[]", nullable: false),
                    Warnings = table.Column<List<string>>(type: "text[]", nullable: false),
                    UserStoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStoryQuality", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStoryQuality_user_stories_UserStoryId",
                        column: x => x.UserStoryId,
                        principalTable: "user_stories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JiraFields_UserStoryId",
                table: "JiraFields",
                column: "UserStoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_story_source_refs_user_story_id",
                table: "user_story_source_refs",
                column: "user_story_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserStoryQuality_UserStoryId",
                table: "UserStoryQuality",
                column: "UserStoryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JiraFields");

            migrationBuilder.DropTable(
                name: "user_story_source_refs");

            migrationBuilder.DropTable(
                name: "UserStoryQuality");

            migrationBuilder.DropColumn(
                name: "DeduplicationKey",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "SourceRequirementId",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "StoryPoints",
                table: "user_stories");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "user_stories");

            // Convert jsonb back to text[]
            migrationBuilder.Sql(
                @"ALTER TABLE user_stories 
                  ALTER COLUMN acceptance_criteria TYPE text[] USING 
                  CASE 
                    WHEN acceptance_criteria::text = '[]' THEN ARRAY[]::text[]
                    ELSE array(SELECT jsonb_array_elements_text(acceptance_criteria))
                  END");
        }
    }
}
