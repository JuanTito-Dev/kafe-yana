using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fristconarrgleodevarcionesporidelaborado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "fx_varicion_nombre",
                table: "Variacion");

            migrationBuilder.CreateIndex(
                name: "fx_varicion_nombre",
                table: "Variacion",
                columns: new[] { "Nombre", "Id_Elaborado" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "fx_varicion_nombre",
                table: "Variacion");

            migrationBuilder.CreateIndex(
                name: "fx_varicion_nombre",
                table: "Variacion",
                column: "Nombre",
                unique: true);
        }
    }
}
