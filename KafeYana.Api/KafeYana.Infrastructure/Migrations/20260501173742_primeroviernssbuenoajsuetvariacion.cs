using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class primeroviernssbuenoajsuetvariacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ajuste_Insumo_Id_Insumo",
                table: "Ajuste");

            migrationBuilder.DropForeignKey(
                name: "FK_Ajuste_Insumo_Id_InsumoNuevo",
                table: "Ajuste");

            migrationBuilder.DropForeignKey(
                name: "FK_Ajuste_Opcion_Id_Opcion",
                table: "Ajuste");

            migrationBuilder.DropForeignKey(
                name: "FK_Opcion_Variacion_Id_variacion",
                table: "Opcion");

            migrationBuilder.DropForeignKey(
                name: "FK_Variacion_Elaborado_Id_Elaborado",
                table: "Variacion");

            migrationBuilder.AddForeignKey(
                name: "fx_ajuste_insumoNevo",
                table: "Ajuste",
                column: "Id_InsumoNuevo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fx_ajuste_insumobase",
                table: "Ajuste",
                column: "Id_Insumo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fx_ajuste_opcion",
                table: "Ajuste",
                column: "Id_Opcion",
                principalTable: "Opcion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fx_opcion_variacion",
                table: "Opcion",
                column: "Id_variacion",
                principalTable: "Variacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fx_variacion_elaborado",
                table: "Variacion",
                column: "Id_Elaborado",
                principalTable: "Elaborado",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fx_ajuste_insumoNevo",
                table: "Ajuste");

            migrationBuilder.DropForeignKey(
                name: "fx_ajuste_insumobase",
                table: "Ajuste");

            migrationBuilder.DropForeignKey(
                name: "fx_ajuste_opcion",
                table: "Ajuste");

            migrationBuilder.DropForeignKey(
                name: "fx_opcion_variacion",
                table: "Opcion");

            migrationBuilder.DropForeignKey(
                name: "fx_variacion_elaborado",
                table: "Variacion");

            migrationBuilder.AddForeignKey(
                name: "FK_Ajuste_Insumo_Id_Insumo",
                table: "Ajuste",
                column: "Id_Insumo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ajuste_Insumo_Id_InsumoNuevo",
                table: "Ajuste",
                column: "Id_InsumoNuevo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ajuste_Opcion_Id_Opcion",
                table: "Ajuste",
                column: "Id_Opcion",
                principalTable: "Opcion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Opcion_Variacion_Id_variacion",
                table: "Opcion",
                column: "Id_variacion",
                principalTable: "Variacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Variacion_Elaborado_Id_Elaborado",
                table: "Variacion",
                column: "Id_Elaborado",
                principalTable: "Elaborado",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
