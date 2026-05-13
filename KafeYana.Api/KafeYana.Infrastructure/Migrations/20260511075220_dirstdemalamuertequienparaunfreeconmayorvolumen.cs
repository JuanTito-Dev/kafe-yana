using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dirstdemalamuertequienparaunfreeconmayorvolumen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbiertaPor",
                table: "CajaHistorial",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CerradaPor",
                table: "CajaHistorial",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbiertaPor",
                table: "CajaHistorial");

            migrationBuilder.DropColumn(
                name: "CerradaPor",
                table: "CajaHistorial");
        }
    }
}
