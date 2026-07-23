using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DetalleRondaUnidadMedida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CodigoUnidadMedida",
                table: "Detalle_Ronda",
                type: "integer",
                nullable: false,
                defaultValue: 57);

            migrationBuilder.Sql("""
                UPDATE "Detalle_Ronda" dr
                SET "CodigoUnidadMedida" = c."CodigoUnidadMedida"
                FROM "Producto" p
                INNER JOIN "Comprado" c ON c."Id_Producto" = p."Id"
                WHERE dr."Id_Producto" = p."Id"
                  AND p."Tipo" = 'Comprado'
                  AND c."CodigoUnidadMedida" > 0;
                """);

            migrationBuilder.Sql("""
                UPDATE "Detalle_Ronda" dr
                SET "CodigoUnidadMedida" = e."CodigoUnidadMedida"
                FROM "Producto" p
                INNER JOIN "Elaborado" e ON e."Id_Producto" = p."Id"
                WHERE dr."Id_Producto" = p."Id"
                  AND p."Tipo" = 'Elaborado'
                  AND e."CodigoUnidadMedida" > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoUnidadMedida",
                table: "Detalle_Ronda");
        }
    }
}
