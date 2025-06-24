using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassSchedulingSys.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSections_CollegeCourse_CollegeCourseId",
                table: "ClassSections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CollegeCourse",
                table: "CollegeCourse");

            migrationBuilder.RenameTable(
                name: "CollegeCourse",
                newName: "CollegeCourses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CollegeCourses",
                table: "CollegeCourses",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSections_CollegeCourses_CollegeCourseId",
                table: "ClassSections",
                column: "CollegeCourseId",
                principalTable: "CollegeCourses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSections_CollegeCourses_CollegeCourseId",
                table: "ClassSections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CollegeCourses",
                table: "CollegeCourses");

            migrationBuilder.RenameTable(
                name: "CollegeCourses",
                newName: "CollegeCourse");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CollegeCourse",
                table: "CollegeCourse",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSections_CollegeCourse_CollegeCourseId",
                table: "ClassSections",
                column: "CollegeCourseId",
                principalTable: "CollegeCourse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
