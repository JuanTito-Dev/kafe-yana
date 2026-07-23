using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPagoParcialAbonos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CantidadPagada",
                table: "Detalle_Ronda",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PedidoAbono",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CodigoMetodoPago = table.Column<int>(type: "integer", nullable: false),
                    EsPagoFinal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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
                    Id_Detalle = table.Column<int>(type: "integer", nullable: false),
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
                        onDelete: ReferentialAction.Restrict);
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidoAbonoDetalle");

            migrationBuilder.DropTable(
                name: "PedidoAbono");

            migrationBuilder.DropColumn(
                name: "CantidadPagada",
                table: "Detalle_Ronda");
        }
    }
}
