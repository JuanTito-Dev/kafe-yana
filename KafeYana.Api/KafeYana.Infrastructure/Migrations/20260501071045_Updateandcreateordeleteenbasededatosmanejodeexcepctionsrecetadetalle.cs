using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updateandcreateordeleteenbasededatosmanejodeexcepctionsrecetadetalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Insumo_Id_insumo",
                table: "Detalle");

            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Receta_Id_receta",
                table: "Detalle");

            migrationBuilder.AddForeignKey(
                name: "fx_detalle_insumo",
                table: "Detalle",
                column: "Id_insumo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fx_detalle_receta",
                table: "Detalle",
                column: "Id_receta",
                principalTable: "Receta",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fx_detalle_insumo",
                table: "Detalle");

            migrationBuilder.DropForeignKey(
                name: "fx_detalle_receta",
                table: "Detalle");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Insumo_Id_insumo",
                table: "Detalle",
                column: "Id_insumo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Receta_Id_receta",
                table: "Detalle",
                column: "Id_receta",
                principalTable: "Receta",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
