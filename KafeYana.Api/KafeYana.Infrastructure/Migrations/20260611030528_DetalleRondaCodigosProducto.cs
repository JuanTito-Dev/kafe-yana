using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DetalleRondaCodigosProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Detalle_Ronda",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CodigoSin",
                table: "Detalle_Ronda",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Detalle_Ronda" dr
                SET "Codigo" = CASE
                        WHEN COALESCE(NULLIF(TRIM(p."Codigo"), ''), '') = '' THEN LPAD(p."Id"::text, 5, '0')
                        ELSE TRIM(p."Codigo")
                    END,
                    "CodigoSin" = COALESCE(TRIM(p."CodigoSin"), '')
                FROM "Producto" p
                WHERE dr."Id_Producto" = p."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Detalle_Ronda");

            migrationBuilder.DropColumn(
                name: "CodigoSin",
                table: "Detalle_Ronda");
        }
    }
}
