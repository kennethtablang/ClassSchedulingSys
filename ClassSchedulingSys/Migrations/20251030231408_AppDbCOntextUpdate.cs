using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassSchedulingSys.Migrations
{
    /// <inheritdoc />
    public partial class AppDbCOntextUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeID",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmployeeID",
                table: "AspNetUsers",
                column: "EmployeeID",
                unique: true,
                filter: "[EmployeeID] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_EmployeeID",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmployeeID",
                table: "AspNetUsers");
        }
    }
}
