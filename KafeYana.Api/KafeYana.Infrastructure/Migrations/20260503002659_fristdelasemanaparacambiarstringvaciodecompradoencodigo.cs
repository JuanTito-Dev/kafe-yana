using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fristdelasemanaparacambiarstringvaciodecompradoencodigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "codigo_producto_comprado",
                table: "Comprado");

            migrationBuilder.CreateIndex(
                name: "codigo_producto_comprado",
                table: "Comprado",
                column: "Codigo_barra",
                unique: true,
                filter: "\"Codigo_barra\" IS NOT NULL AND \"Codigo_barra\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "codigo_producto_comprado",
                table: "Comprado");

            migrationBuilder.CreateIndex(
                name: "codigo_producto_comprado",
                table: "Comprado",
                column: "Codigo_barra",
                unique: true);
        }
    }
}
