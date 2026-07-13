using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class frtisdenadieconfuerazasconcadarinco2punto0contodo76534 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pago",
                table: "Venta");

            migrationBuilder.AddColumn<decimal>(
                name: "PagoEfectivo",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PagoQr",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PagoTarjeta",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEfectivo",
                table: "CajaHistorial",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalQr",
                table: "CajaHistorial",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTarjeta",
                table: "CajaHistorial",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEfectivo",
                table: "Caja",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalQr",
                table: "Caja",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTarjeta",
                table: "Caja",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PagoEfectivo",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "PagoQr",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "PagoTarjeta",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "TotalEfectivo",
                table: "CajaHistorial");

            migrationBuilder.DropColumn(
                name: "TotalQr",
                table: "CajaHistorial");

            migrationBuilder.DropColumn(
                name: "TotalTarjeta",
                table: "CajaHistorial");

            migrationBuilder.DropColumn(
                name: "TotalEfectivo",
                table: "Caja");

            migrationBuilder.DropColumn(
                name: "TotalQr",
                table: "Caja");

            migrationBuilder.DropColumn(
                name: "TotalTarjeta",
                table: "Caja");

            migrationBuilder.AddColumn<string>(
                name: "Pago",
                table: "Venta",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
