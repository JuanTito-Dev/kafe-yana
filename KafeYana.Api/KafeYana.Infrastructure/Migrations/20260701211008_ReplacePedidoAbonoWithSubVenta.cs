using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePedidoAbonoWithSubVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidoAbonoDetalle");

            migrationBuilder.DropTable(
                name: "PedidoAbono");

            migrationBuilder.RenameColumn(
                name: "CantidadPagada",
                table: "Detalle_Ronda",
                newName: "CantidadDescontada");

            migrationBuilder.CreateTable(
                name: "SubVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CodigoMetodoPago = table.Column<int>(type: "integer", nullable: false),
                    EsPagoFinal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Cajero = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Facturada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Id_Venta = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubVenta", x => x.Id);
                    table.ForeignKey(
                        name: "fk_subventa_pedido",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subventa_venta",
                        column: x => x.Id_Venta,
                        principalTable: "Venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SubVentaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_SubVenta = table.Column<int>(type: "integer", nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Nombre_Producto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: ""),
                    CodigoSin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    CodigoUnidadMedida = table.Column<int>(type: "integer", nullable: false, defaultValue: 57),
                    OrigenRondaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubVentaDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "fk_subventadetalle_subventa",
                        column: x => x.Id_SubVenta,
                        principalTable: "SubVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubVenta_Facturada",
                table: "SubVenta",
                column: "Facturada");

            migrationBuilder.CreateIndex(
                name: "IX_SubVenta_Id_Pedido",
                table: "SubVenta",
                column: "Id_Pedido");

            migrationBuilder.CreateIndex(
                name: "IX_SubVenta_Id_Venta",
                table: "SubVenta",
                column: "Id_Venta");

            migrationBuilder.CreateIndex(
                name: "IX_SubVentaDetalle_Id_SubVenta",
                table: "SubVentaDetalle",
                column: "Id_SubVenta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubVentaDetalle");

            migrationBuilder.DropTable(
                name: "SubVenta");

            migrationBuilder.RenameColumn(
                name: "CantidadDescontada",
                table: "Detalle_Ronda",
                newName: "CantidadPagada");

            migrationBuilder.CreateTable(
                name: "PedidoAbono",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false),
                    CodigoMetodoPago = table.Column<int>(type: "integer", nullable: false),
                    EsPagoFinal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoAbono", x => x.Id);
                    table.ForeignKey(
                        name: "fk_pedidoabono_pedido",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoAbonoDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Abono = table.Column<int>(type: "integer", nullable: false),
                    Id_Detalle = table.Column<int>(type: "integer", nullable: true),
                    Cantidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoAbonoDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "fk_pedidoabonodetalle_abono",
                        column: x => x.Id_Abono,
                        principalTable: "PedidoAbono",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pedidoabonodetalle_detalle",
                        column: x => x.Id_Detalle,
                        principalTable: "Detalle_Ronda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoAbono_Id_Pedido",
                table: "PedidoAbono",
                column: "Id_Pedido");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoAbonoDetalle_Id_Abono",
                table: "PedidoAbonoDetalle",
                column: "Id_Abono");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoAbonoDetalle_Id_Detalle",
                table: "PedidoAbonoDetalle",
                column: "Id_Detalle");
        }
    }
}
