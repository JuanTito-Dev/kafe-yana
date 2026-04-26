using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoOpcionToDetalleRondaOpcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoExtra",
                table: "Detalle_Ronda_Opcion",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoOpcion",
                table: "Detalle_Ronda_Opcion",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "normal");

            migrationBuilder.AddColumn<string>(
                name: "ValorAnterior",
                table: "Detalle_Ronda_Opcion",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoExtra",
                table: "Detalle_Ronda_Opcion");

            migrationBuilder.DropColumn(
                name: "TipoOpcion",
                table: "Detalle_Ronda_Opcion");

            migrationBuilder.DropColumn(
                name: "ValorAnterior",
                table: "Detalle_Ronda_Opcion");
        }
    }
}
