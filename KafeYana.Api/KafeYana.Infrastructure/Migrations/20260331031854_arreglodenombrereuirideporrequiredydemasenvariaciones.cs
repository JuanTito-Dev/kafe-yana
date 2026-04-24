using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class arreglodenombrereuirideporrequiredydemasenvariaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Requirido",
                table: "Variacion",
                newName: "Requerido");

            migrationBuilder.RenameIndex(
                name: "IX_Variacion_Requirido",
                table: "Variacion",
                newName: "IX_Variacion_Requerido");

            migrationBuilder.AlterColumn<int>(
                name: "Id_InsumoNuevo",
                table: "Ajuste",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Requerido",
                table: "Variacion",
                newName: "Requirido");

            migrationBuilder.RenameIndex(
                name: "IX_Variacion_Requerido",
                table: "Variacion",
                newName: "IX_Variacion_Requirido");

            migrationBuilder.AlterColumn<int>(
                name: "Id_InsumoNuevo",
                table: "Ajuste",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
