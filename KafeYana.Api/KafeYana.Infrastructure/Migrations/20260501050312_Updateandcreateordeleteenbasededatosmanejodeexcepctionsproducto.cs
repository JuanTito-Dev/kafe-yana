using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updateandcreateordeleteenbasededatosmanejodeexcepctionsproducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comprado_Producto_Id_Producto",
                table: "Comprado");

            migrationBuilder.DropIndex(
                name: "IX_Comprado_Codigo_barra",
                table: "Comprado");

            migrationBuilder.CreateIndex(
                name: "codigo_producto_comprado",
                table: "Comprado",
                column: "Codigo_barra",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fx_producto_comprado",
                table: "Comprado",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fx_producto_comprado",
                table: "Comprado");

            migrationBuilder.DropIndex(
                name: "codigo_producto_comprado",
                table: "Comprado");

            migrationBuilder.CreateIndex(
                name: "IX_Comprado_Codigo_barra",
                table: "Comprado",
                column: "Codigo_barra");

            migrationBuilder.AddForeignKey(
                name: "FK_Comprado_Producto_Id_Producto",
                table: "Comprado",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
