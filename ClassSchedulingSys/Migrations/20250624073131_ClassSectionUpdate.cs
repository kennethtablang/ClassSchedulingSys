using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassSchedulingSys.Migrations
{
    /// <inheritdoc />
    public partial class ClassSectionUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ClassSections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ClassSections",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
