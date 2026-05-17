using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VentaSecuencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS venta_codigo_seq;");

            // Initialize the sequence at current total venta count + 1 to avoid collisions
            migrationBuilder.Sql(
                "SELECT setval('venta_codigo_seq', GREATEST(COALESCE((SELECT COUNT(*) FROM \"Venta\"), 0), 1), false);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS venta_codigo_seq;");
        }
    }
}
