using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updateandcreateordeleteenbasededatosmanejodeexcepctionsrecetadetallenombrepersonalizados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Receta_Id_Elaborado",
                table: "Receta",
                newName: "ix_receta_id_elaborado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_receta_id_elaborado",
                table: "Receta",
                newName: "IX_Receta_Id_Elaborado");
        }
    }
}
