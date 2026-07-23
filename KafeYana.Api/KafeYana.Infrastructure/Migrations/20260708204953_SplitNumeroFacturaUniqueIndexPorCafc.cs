using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitNumeroFacturaUniqueIndexPorCafc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venta_NumeroFactura",
                table: "Venta");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_NumeroFactura_Cafc",
                table: "Venta",
                column: "NumeroFactura",
                unique: true,
                filter: "\"NumeroFactura\" IS NOT NULL AND \"Cafc\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venta_NumeroFactura_Cafc",
                table: "Venta");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_NumeroFactura",
                table: "Venta",
                column: "NumeroFactura",
                unique: true,
                filter: "\"NumeroFactura\" IS NOT NULL");
        }
    }
}
