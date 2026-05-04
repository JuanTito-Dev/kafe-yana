using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fristdiadehoyclientesconfigconventa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mesa_Pedido_Id_Pedido",
                table: "Mesa");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedido_Clientes_Id_Cliente",
                table: "Pedido");

            migrationBuilder.DropForeignKey(
                name: "FK_Ronda_Pedido_Id_Pedido",
                table: "Ronda");

            migrationBuilder.RenameIndex(
                name: "IX_Mesa_Nombre",
                table: "Mesa",
                newName: "Unique_mesa_nombre");

            migrationBuilder.AddForeignKey(
                name: "fx_pedido_mesa",
                table: "Mesa",
                column: "Id_Pedido",
                principalTable: "Pedido",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fx_pedido_cliente",
                table: "Pedido",
                column: "Id_Cliente",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fx_pedido_ronda",
                table: "Ronda",
                column: "Id_Pedido",
                principalTable: "Pedido",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fx_pedido_mesa",
                table: "Mesa");

            migrationBuilder.DropForeignKey(
                name: "fx_pedido_cliente",
                table: "Pedido");

            migrationBuilder.DropForeignKey(
                name: "fx_pedido_ronda",
                table: "Ronda");

            migrationBuilder.RenameIndex(
                name: "Unique_mesa_nombre",
                table: "Mesa",
                newName: "IX_Mesa_Nombre");

            migrationBuilder.AddForeignKey(
                name: "FK_Mesa_Pedido_Id_Pedido",
                table: "Mesa",
                column: "Id_Pedido",
                principalTable: "Pedido",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedido_Clientes_Id_Cliente",
                table: "Pedido",
                column: "Id_Cliente",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Ronda_Pedido_Id_Pedido",
                table: "Ronda",
                column: "Id_Pedido",
                principalTable: "Pedido",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
