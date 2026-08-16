using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class allownullableSourceUserStoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.RenameColumn(
            //    name: "IsDeleted",
            //    table: "projects",
            //    newName: "is_deleted");

            migrationBuilder.AlterColumn<string>(
                name: "SourceUserStoryId",
                table: "user_stories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.RenameColumn(
            //    name: "is_deleted",
            //    table: "projects",
            //    newName: "IsDeleted");

            migrationBuilder.AlterColumn<string>(
                name: "SourceUserStoryId",
                table: "user_stories",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
