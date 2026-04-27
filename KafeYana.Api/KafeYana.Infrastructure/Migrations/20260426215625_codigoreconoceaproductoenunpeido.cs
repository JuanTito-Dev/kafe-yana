using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class codigoreconoceaproductoenunpeido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Ronda_Producto_Id_Producto",
                table: "Detalle_Ronda");

            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Ronda_Producto_ProductoId",
                table: "Detalle_Ronda");

            migrationBuilder.DropForeignKey(
                name: "FK_Producto_Categorias_Categoria_Id",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Detalle_Ronda_ProductoId",
                table: "Detalle_Ronda");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "Detalle_Ronda");

            migrationBuilder.AddForeignKey(
                name: "id_Pedido_compra",
                table: "Detalle_Ronda",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "id_categoria_producto",
                table: "Producto",
                column: "Categoria_Id",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "id_Pedido_compra",
                table: "Detalle_Ronda");

            migrationBuilder.DropForeignKey(
                name: "id_categoria_producto",
                table: "Producto");

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "Detalle_Ronda",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_ProductoId",
                table: "Detalle_Ronda",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Ronda_Producto_Id_Producto",
                table: "Detalle_Ronda",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Ronda_Producto_ProductoId",
                table: "Detalle_Ronda",
                column: "ProductoId",
                principalTable: "Producto",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_Categorias_Categoria_Id",
                table: "Producto",
                column: "Categoria_Id",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
