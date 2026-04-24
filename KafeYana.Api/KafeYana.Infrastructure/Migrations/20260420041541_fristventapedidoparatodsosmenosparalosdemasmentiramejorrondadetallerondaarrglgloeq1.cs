using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fristventapedidoparatodsosmenosparalosdemasmentiramejorrondadetallerondaarrglgloeq1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Total",
                table: "Detalle_rondas",
                newName: "Precio");

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "Ronda",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Nombre_Producto",
                table: "Detalle_rondas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "Ronda");

            migrationBuilder.DropColumn(
                name: "Nombre_Producto",
                table: "Detalle_rondas");

            migrationBuilder.RenameColumn(
                name: "Precio",
                table: "Detalle_rondas",
                newName: "Total");
        }
    }
}
