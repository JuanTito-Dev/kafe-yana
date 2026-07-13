using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <summary>
    /// Data-only migration: reescribe los registros contingencia viejos de
    /// <c>Venta.TipoEmision = 4</c> a <c>TipoEmision = 2</c>, alineándolos con
    /// el codigoEmision oficial del SIAT para modalidad computarizada fuera de línea
    /// (Resolución Normativa 102100000028).
    ///
    /// El schema no cambia — solo se actualizan datos. La rama del flujo online
    /// (<c>TipoEmision = 1</c>) no se toca. Si no hay registros contingencia
    /// previos, el UPDATE es no-op.
    ///
    /// Equivalente para <c>NotaAjuste.TipoEmision</c> cuando se implemente el
    /// plan de contingencia de Notas de Ajuste (Pieza A1-A6).
    /// </summary>
    public partial class VentaTipoEmisionContingencia4to2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Solo afecta las filas contingencia. NO toca TipoEmision=1 (online)
            // ni NULL (ventas pre-contingencia sin envío).
            migrationBuilder.Sql(
                "UPDATE \"Venta\" SET \"TipoEmision\" = 2 WHERE \"TipoEmision\" = 4;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reversible — si necesitamos volver atrás, recuperamos el 4.
            migrationBuilder.Sql(
                "UPDATE \"Venta\" SET \"TipoEmision\" = 4 WHERE \"TipoEmision\" = 2;");
        }
    }
}