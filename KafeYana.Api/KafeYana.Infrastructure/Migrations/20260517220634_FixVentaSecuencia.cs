using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixVentaSecuencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Resets the sequence so the next nextval returns COUNT(*) + 1,
            // preventing collisions with existing venta codes.
            // The original migration used (is_called = false) which made nextval
            // return COUNT(*) instead of COUNT(*) + 1 — causing duplicates.
            migrationBuilder.Sql(
                "SELECT setval('venta_codigo_seq', GREATEST(COALESCE((SELECT COUNT(*) FROM \"Venta\"), 0), 1), true);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "SELECT setval('venta_codigo_seq', GREATEST(COALESCE((SELECT COUNT(*) FROM \"Venta\"), 0), 1), false);");
        }
    }
}
