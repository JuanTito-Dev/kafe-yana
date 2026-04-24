using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ventasydeatlleventaparakafeyanaenestemomento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Venta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Cliente = table.Column<string>(type: "text", nullable: false),
                    Cajero = table.Column<string>(type: "text", nullable: false),
                    Productos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Pago = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Detalle_venta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_venta = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_venta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetalleVenta_Venta",
                        column: x => x.Id_venta,
                        principalTable: "Venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_venta_Id_venta",
                table: "Detalle_venta",
                column: "Id_venta");

            migrationBuilder.CreateIndex(
                name: "Codigo-repetido",
                table: "Venta",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Cajero",
                table: "Venta",
                column: "Cajero");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Cliente",
                table: "Venta",
                column: "Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Estado",
                table: "Venta",
                column: "Estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Detalle_venta");

            migrationBuilder.DropTable(
                name: "Venta");
        }
    }
}
