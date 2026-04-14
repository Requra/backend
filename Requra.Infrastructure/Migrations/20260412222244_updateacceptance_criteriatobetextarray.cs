using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateacceptance_criteriatobetextarray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProjectType",
                table: "projects",
                newName: "Project_type");

            //    migrationBuilder.AlterColumn<List<string>>(
            //        name: "acceptance_criteria",
            //        table: "user_stories",
            //        type: "text[]",
            //        nullable: false,
            //        oldClrType: typeof(string),
            //        oldType: "text",
            //        oldNullable: true);
            //}
            migrationBuilder.Sql(@"
                 ALTER TABLE user_stories 
                 ALTER COLUMN acceptance_criteria 
                 TYPE text[] 
                 USING 
                     CASE 
                         WHEN acceptance_criteria IS NULL THEN NULL
                         ELSE ARRAY[acceptance_criteria]
                     END;
                 ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Project_type",
                table: "projects",
                newName: "ProjectType");

            //migrationBuilder.AlterColumn<string>(
            //    name: "acceptance_criteria",
            //    table: "user_stories",
            //    type: "text",
            //    nullable: true,
            //    oldClrType: typeof(List<string>),
            //    oldType: "text[]");
            migrationBuilder.Sql(@"
                 ALTER TABLE user_stories 
                 ALTER COLUMN acceptance_criteria 
                 TYPE text 
                 USING array_to_string(acceptance_criteria, ',');
                ");
        }
    }
}
