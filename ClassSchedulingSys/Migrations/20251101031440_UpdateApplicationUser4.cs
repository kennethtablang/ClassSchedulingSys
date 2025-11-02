using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassSchedulingSys.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApplicationUser4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TwoFactorAttempts",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwoFactorCodeExpiry",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorCodeHash",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFactorAttempts",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorCodeExpiry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorCodeHash",
                table: "AspNetUsers");
        }
    }
}
