using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addStartandEndToChunk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EndedAtMs",
                table: "recording_chunks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StartedAtMs",
                table: "recording_chunks",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndedAtMs",
                table: "recording_chunks");

            migrationBuilder.DropColumn(
                name: "StartedAtMs",
                table: "recording_chunks");
        }
    }
}
