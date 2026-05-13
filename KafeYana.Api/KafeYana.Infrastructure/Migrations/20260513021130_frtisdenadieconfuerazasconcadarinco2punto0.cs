using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class frtisdenadieconfuerazasconcadarinco2punto0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "fx_opcion_nombre",
                table: "Opcion");

            migrationBuilder.CreateIndex(
                name: "IX_Opcion_Nombre",
                table: "Opcion",
                column: "Nombre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Opcion_Nombre",
                table: "Opcion");

            migrationBuilder.CreateIndex(
                name: "fx_opcion_nombre",
                table: "Opcion",
                column: "Nombre",
                unique: true);
        }
    }
}
