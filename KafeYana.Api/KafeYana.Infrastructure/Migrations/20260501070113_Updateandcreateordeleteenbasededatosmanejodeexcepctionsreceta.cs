using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updateandcreateordeleteenbasededatosmanejodeexcepctionsreceta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receta_Elaborado_Id_Elaborado",
                table: "Receta");

            migrationBuilder.RenameIndex(
                name: "IX_Receta_Nombre",
                table: "Receta",
                newName: "receta_nombre");

            migrationBuilder.AddForeignKey(
                name: "fx_elaborado_receta",
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
                name: "fx_elaborado_receta",
                table: "Receta");

            migrationBuilder.RenameIndex(
                name: "receta_nombre",
                table: "Receta",
                newName: "IX_Receta_Nombre");

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
