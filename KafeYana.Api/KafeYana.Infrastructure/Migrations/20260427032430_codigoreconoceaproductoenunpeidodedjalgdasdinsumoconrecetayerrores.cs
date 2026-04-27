using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class codigoreconoceaproductoenunpeidodedjalgdasdinsumoconrecetayerrores : Migration
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

            migrationBuilder.AddForeignKey(
                name: "Insumo_Nuevo_opcion",
                table: "Ajuste",
                column: "Id_InsumoNuevo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "Insumo_base_opcion",
                table: "Ajuste",
                column: "Id_Insumo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Insumo_Nuevo_opcion",
                table: "Ajuste");

            migrationBuilder.DropForeignKey(
                name: "Insumo_base_opcion",
                table: "Ajuste");

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
        }
    }
}
