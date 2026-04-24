using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class agregarajusteparalavariacionconeliminacionmasajustedecantidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TipoAjuste",
                table: "Ajuste",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "Id_InsumoNuevo",
                table: "Ajuste",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Receta_Nombre",
                table: "Receta",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ajuste_Id_Insumo",
                table: "Ajuste",
                column: "Id_Insumo");

            migrationBuilder.CreateIndex(
                name: "IX_Ajuste_Id_InsumoNuevo",
                table: "Ajuste",
                column: "Id_InsumoNuevo");

            migrationBuilder.AddForeignKey(
                name: "FK_Ajuste_Insumo_Id_InsumoNuevo",
                table: "Ajuste",
                column: "Id_InsumoNuevo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ajuste_Insumo_Id_InsumoNuevo",
                table: "Ajuste");

            migrationBuilder.DropIndex(
                name: "IX_Receta_Nombre",
                table: "Receta");

            migrationBuilder.DropIndex(
                name: "IX_Ajuste_Id_Insumo",
                table: "Ajuste");

            migrationBuilder.DropIndex(
                name: "IX_Ajuste_Id_InsumoNuevo",
                table: "Ajuste");

            migrationBuilder.DropColumn(
                name: "Id_InsumoNuevo",
                table: "Ajuste");

            migrationBuilder.AlterColumn<string>(
                name: "TipoAjuste",
                table: "Ajuste",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
