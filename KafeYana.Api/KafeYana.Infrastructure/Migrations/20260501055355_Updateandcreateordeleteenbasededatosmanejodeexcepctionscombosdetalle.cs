using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updateandcreateordeleteenbasededatosmanejodeexcepctionscombosdetalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_promocion_Producto_Id_Producto",
                table: "Detalle_promocion");

            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_promocion_Promocion_Id_Promocion",
                table: "Detalle_promocion");

            migrationBuilder.DropForeignKey(
                name: "FK_Elaborado_Producto_Id_Producto",
                table: "Elaborado");

            migrationBuilder.AddForeignKey(
                name: "fx",
                table: "Detalle_promocion",
                column: "Id_Promocion",
                principalTable: "Promocion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fx_detallecombo_producto",
                table: "Detalle_promocion",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fx_producto_elaborado",
                table: "Elaborado",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fx",
                table: "Detalle_promocion");

            migrationBuilder.DropForeignKey(
                name: "fx_detallecombo_producto",
                table: "Detalle_promocion");

            migrationBuilder.DropForeignKey(
                name: "fx_producto_elaborado",
                table: "Elaborado");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_promocion_Producto_Id_Producto",
                table: "Detalle_promocion",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_promocion_Promocion_Id_Promocion",
                table: "Detalle_promocion",
                column: "Id_Promocion",
                principalTable: "Promocion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Elaborado_Producto_Id_Producto",
                table: "Elaborado",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
