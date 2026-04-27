using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class codigoreconoceaproductoenunpeidodedjalgdasdinsumo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Insumo_Id_insumo",
                table: "Detalle");

            migrationBuilder.DropForeignKey(
                name: "FK_Receta_Elaborado_Id_Elaborado",
                table: "Receta");

            migrationBuilder.RenameIndex(
                name: "IX_Insumo_Nombre",
                table: "Insumo",
                newName: "Nombre_insumo");

            migrationBuilder.AddForeignKey(
                name: "Id_detalle_id_insumo",
                table: "Detalle",
                column: "Id_insumo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "receta_id_producto_id",
                table: "Receta",
                column: "Id_Elaborado",
                principalTable: "Elaborado",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Id_detalle_id_insumo",
                table: "Detalle");

            migrationBuilder.DropForeignKey(
                name: "receta_id_producto_id",
                table: "Receta");

            migrationBuilder.RenameIndex(
                name: "Nombre_insumo",
                table: "Insumo",
                newName: "IX_Insumo_Nombre");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Insumo_Id_insumo",
                table: "Detalle",
                column: "Id_insumo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Receta_Elaborado_Id_Elaborado",
                table: "Receta",
                column: "Id_Elaborado",
                principalTable: "Elaborado",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
