using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassSchedulingSys.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_SubjectCode_YearLevel_CollegeCourseId",
                table: "Subjects");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectCode_YearLevel_CollegeCourseId",
                table: "Subjects",
                columns: new[] { "SubjectCode", "YearLevel", "CollegeCourseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_SubjectCode_YearLevel_CollegeCourseId",
                table: "Subjects");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectCode_YearLevel_CollegeCourseId",
                table: "Subjects",
                columns: new[] { "SubjectCode", "YearLevel", "CollegeCourseId" },
                unique: true,
                filter: "[IsActive] = 1");
        }
    }
}
