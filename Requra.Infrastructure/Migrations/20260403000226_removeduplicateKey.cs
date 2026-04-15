using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removeduplicateKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_project_members_projects_ProjectId1",
            //    table: "project_members");

            //migrationBuilder.DropIndex(
            //    name: "IX_project_members_ProjectId1",
            //    table: "project_members");

            //migrationBuilder.DropColumn(
            //    name: "ProjectId1",
            //    table: "project_members");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<Guid>(
            //    name: "ProjectId1",
            //    table: "project_members",
            //    type: "uuid",
            //    nullable: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_project_members_ProjectId1",
            //    table: "project_members",
            //    column: "ProjectId1");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_project_members_projects_ProjectId1",
            //    table: "project_members",
            //    column: "ProjectId1",
            //    principalTable: "projects",
            //    principalColumn: "id");
        }
    }
}
