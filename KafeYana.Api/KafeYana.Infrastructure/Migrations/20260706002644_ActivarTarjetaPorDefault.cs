using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActivarTarjetaPorDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Activa Tarjeta (código SIN 2) como método de pago habilitado.
            // No-op si la fila todavía no existe (se crea ya activa por el
            // nuevo seed default en el próximo sync — ver MetodoPagoSiatCatalogo).
            migrationBuilder.Sql(
                "UPDATE \"CatMetodosPago\" SET \"Activo\" = true WHERE \"Codigo\" = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"CatMetodosPago\" SET \"Activo\" = false WHERE \"Codigo\" = 2;");
        }
    }
}
