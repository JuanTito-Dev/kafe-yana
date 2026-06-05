using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUbicacionToDetalleVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ubicacion",
                table: "Detalle_venta",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ubicacion",
                table: "Detalle_venta");
        }
    }
}
