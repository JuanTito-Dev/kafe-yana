using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fristventaokardexokeymaestrodegoma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                table: "Venta",
                type: "text",
                nullable: false,
                defaultValue: "VTA-LEGACY",
                oldClrType: typeof(string),
                oldType: "text",
                oldComputedColumnSql: "'VTA-' || CAST(\"Id\" AS VARCHAR)");

            migrationBuilder.CreateTable(
                name: "Movimiento_Producto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Costo_Unitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Stock_resultante = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimiento_Producto", x => x.Id);
                    table.ForeignKey(
                        name: "fx_producto_movimientos",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Movimiento_Producto_Id_Producto",
                table: "Movimiento_Producto",
                column: "Id_Producto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movimiento_Producto");

            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                table: "Venta",
                type: "text",
                nullable: false,
                computedColumnSql: "'VTA-' || CAST(\"Id\" AS VARCHAR)",
                stored: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "VTA-LEGACY");
        }
    }
}
