using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoOpcionAndValorAnteriorToOpcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoOpcion",
                table: "Opcion",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "normal");

            migrationBuilder.AddColumn<string>(
                name: "ValorAnterior",
                table: "Opcion",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoOpcion",
                table: "Opcion");

            migrationBuilder.DropColumn(
                name: "ValorAnterior",
                table: "Opcion");
        }
    }
}
