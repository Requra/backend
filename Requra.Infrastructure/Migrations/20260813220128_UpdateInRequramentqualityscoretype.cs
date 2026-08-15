using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInRequramentqualityscoretype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_requirements_ProjectId",
                table: "requirements");

            migrationBuilder.AlterColumn<double>(
                name: "QualityScore",
                table: "requirements",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_requirements_ProjectId_SourceRequirementId",
                table: "requirements",
                columns: new[] { "ProjectId", "SourceRequirementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_requirements_ProjectId_SourceRequirementId",
                table: "requirements");

            migrationBuilder.AlterColumn<int>(
                name: "QualityScore",
                table: "requirements",
                type: "integer",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_requirements_ProjectId",
                table: "requirements",
                column: "ProjectId");
        }
    }
}
