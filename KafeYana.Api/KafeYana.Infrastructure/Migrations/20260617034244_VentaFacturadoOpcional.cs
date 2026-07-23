using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VentaFacturadoOpcional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venta_NumeroFactura",
                table: "Venta");

            migrationBuilder.AlterColumn<long>(
                name: "NumeroFactura",
                table: "Venta",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<bool>(
                name: "Facturado",
                table: "Venta",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Facturado",
                table: "Venta",
                column: "Facturado");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_NumeroFactura",
                table: "Venta",
                column: "NumeroFactura",
                unique: true,
                filter: "\"NumeroFactura\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venta_Facturado",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_NumeroFactura",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Facturado",
                table: "Venta");

            migrationBuilder.AlterColumn<long>(
                name: "NumeroFactura",
                table: "Venta",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venta_NumeroFactura",
                table: "Venta",
                column: "NumeroFactura",
                unique: true);
        }
    }
}
