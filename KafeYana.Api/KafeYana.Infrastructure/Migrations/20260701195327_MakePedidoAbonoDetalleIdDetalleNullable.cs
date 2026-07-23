using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakePedidoAbonoDetalleIdDetalleNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pedidoabonodetalle_detalle",
                table: "PedidoAbonoDetalle");

            migrationBuilder.AlterColumn<int>(
                name: "Id_Detalle",
                table: "PedidoAbonoDetalle",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "fk_pedidoabonodetalle_detalle",
                table: "PedidoAbonoDetalle",
                column: "Id_Detalle",
                principalTable: "Detalle_Ronda",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pedidoabonodetalle_detalle",
                table: "PedidoAbonoDetalle");

            migrationBuilder.AlterColumn<int>(
                name: "Id_Detalle",
                table: "PedidoAbonoDetalle",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_pedidoabonodetalle_detalle",
                table: "PedidoAbonoDetalle",
                column: "Id_Detalle",
                principalTable: "Detalle_Ronda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
