using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassSchedulingSys.Migrations
{
    /// <inheritdoc />
    public partial class updatesujecthandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "YearLevel",
                table: "Subjects",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectCode_YearLevel_CollegeCourseId",
                table: "Subjects",
                columns: new[] { "SubjectCode", "YearLevel", "CollegeCourseId" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_SubjectCode_YearLevel_CollegeCourseId",
                table: "Subjects");

            migrationBuilder.AlterColumn<string>(
                name: "YearLevel",
                table: "Subjects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
