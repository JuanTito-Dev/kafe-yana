using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updateandcreateordeleteenbasededatosmanejodeexcepctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Producto_Categorias_Categoria_Id",
                table: "Producto");

            migrationBuilder.AddForeignKey(
                name: "fk_productos_categoria",
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
                name: "fk_productos_categoria",
                table: "Producto");

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
