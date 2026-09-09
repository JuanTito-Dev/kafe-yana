using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class tablaCodigosSiatarreglocodigos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_CodigosSiat_Producto_Actividad",
                table: "CodigosSiat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_CodigosSiat_Producto_Actividad",
                table: "CodigosSiat",
                columns: new[] { "CodigoProducto", "CodigoActividad" },
                unique: true);
        }
    }
}
