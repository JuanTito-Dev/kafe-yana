using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <summary>
    /// Versiona los objetos de correlativo de facturación que hasta ahora se creaban
    /// a mano en cada BD (los comentarios de <c>VentaRepositorio</c> /
    /// <c>NotaAjuteRepositorio</c> lo pedían explícitamente). Una BD nueva sin estas
    /// secuencias hacía fallar la PRIMERA factura con
    /// <c>relation "Venta_NumeroFactura_seq" does not exist</c>.
    ///
    /// Además restituye la protección de unicidad del número de factura ONLINE
    /// (<c>Cafc IS NULL</c>), que se perdió en
    /// <c>20260708204953_SplitNumeroFacturaUniqueIndexPorCafc</c> (esa migración
    /// dejó únicamente el índice para facturas con CAFC).
    ///
    /// Todo idempotente (<c>IF NOT EXISTS</c>): en BDs donde ya se corrió el script
    /// manual esta migración es un no-op. Sin impacto en datos: las <c>Venta</c>
    /// existentes tienen <c>NumeroFactura</c> NULL (nunca se facturó), el índice
    /// parcial se construye sobre cero filas.
    /// </summary>
    /// <inheritdoc />
    public partial class AddFacturaCorrelativoSequencesYIndiceUnicoOnline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Secuencias de correlativo ────────────────────────────────────
            migrationBuilder.Sql(@"
                CREATE SEQUENCE IF NOT EXISTS ""Venta_NumeroFactura_seq"" START 1;
                CREATE SEQUENCE IF NOT EXISTS ""Venta_NumeroFacturaCafc_seq"" START 1;
                CREATE SEQUENCE IF NOT EXISTS ""NotaAjuste_Numero_seq"" START 1;
                CREATE SEQUENCE IF NOT EXISTS venta_codigo_seq START 1;
            ");

            // ── Índice único del número de factura ONLINE (Cafc IS NULL) ─────
            // El de contingencia (IX_Venta_NumeroFactura_Cafc, Cafc IS NOT NULL) ya existe.
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Venta_NumeroFactura_Online""
                    ON ""Venta"" (""NumeroFactura"")
                    WHERE ""NumeroFactura"" IS NOT NULL AND ""Cafc"" IS NULL;
            ");

            // ── Limpieza de índice redundante ───────────────────────────────
            // NotaAjuste ya trae IX_NotaAjuste_NumeroNotaCreditoDebito (único parcial,
            // mismo predicado). Un IX_NotaAjuste_Numero_Online creado a mano sobra.
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_NotaAjuste_Numero_Online"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Venta_NumeroFactura_Online"";");

            // Las secuencias NO se borran: quitarlas rompería los correlativos ya
            // emitidos al SIN. IX_NotaAjuste_Numero_Online tampoco se recrea (era
            // redundante).
        }
    }
}
