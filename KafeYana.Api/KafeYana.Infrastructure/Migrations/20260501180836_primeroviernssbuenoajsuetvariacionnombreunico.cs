using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class primeroviernssbuenoajsuetvariacionnombreunico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Variacion_Nombre",
                table: "Variacion");

            migrationBuilder.CreateIndex(
                name: "fx_varicion_nombre",
                table: "Variacion",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fx_opcion_nombre",
                table: "Opcion",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "fx_varicion_nombre",
                table: "Variacion");

            migrationBuilder.DropIndex(
                name: "fx_opcion_nombre",
                table: "Opcion");

            migrationBuilder.CreateIndex(
                name: "IX_Variacion_Nombre",
                table: "Variacion",
                column: "Nombre");
        }
    }
}
