using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVentaDescuentoPromocionPermanente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistorialPromocionPermanente_CodigoVenta_Unique",
                table: "HistorialPromocionPermanente");

            migrationBuilder.AddColumn<int>(
                name: "Id_PromocionPermanenteDescuento",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDescuento",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NombrePromocionDescuento",
                table: "Venta",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PorcentajeDescuento",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_Venta_TipoRecompensa",
                table: "HistorialPromocionPermanente",
                columns: new[] { "CodigoVenta", "TipoRecompensa" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistorialPromocionPermanente_Venta_TipoRecompensa",
                table: "HistorialPromocionPermanente");

            migrationBuilder.DropColumn(
                name: "Id_PromocionPermanenteDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "MontoDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "NombrePromocionDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "PorcentajeDescuento",
                table: "Venta");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_CodigoVenta_Unique",
                table: "HistorialPromocionPermanente",
                column: "CodigoVenta",
                unique: true);
        }
    }
}
