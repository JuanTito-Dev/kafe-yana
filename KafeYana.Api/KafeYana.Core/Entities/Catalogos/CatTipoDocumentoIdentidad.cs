namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo paramétrico de tipos de documento de identidad del SIAT,
    /// sincronizado vía <c>sincronizarParametricaTipoDocumentoIdentidad</c>.
    ///
    /// Catálogo UNIVERSAL: aplica a todos los contribuyentes, no se filtra por
    /// actividad económica.
    ///
    /// Lista oficial vigente (jun-2026, devuelta por el SIN):
    ///   1 = CI  - CEDULA DE IDENTIDAD
    ///   2 = CEX - CEDULA DE IDENTIDAD DE EXTRANJERO
    ///   3 = PAS - PASAPORTE
    ///   4 = OD  - OTRO DOCUMENTO DE IDENTIDAD
    ///   5 = NIT - NÚMERO DE IDENTIFICACIÓN TRIBUTARIA
    ///
    /// Se usa para validar <c>codigoTipoDocumentoIdentidad</c> en cada venta
    /// facturada (<c>Venta.CodigoTipoDocumentoIdentidad</c>), se serializa en
    /// el XML de la factura (<c>FacturaXmlGenerator</c> / <c>NotaAjusteXmlGenerator</c>)
    /// y se imprime en el ticket (<c>FacturaTicketBuilder</c>).
    ///
    /// Se refresca diariamente a las 08:10 BOT por
    /// <c>SincronizacionTipoDocumentoIdentidadHostedService</c>.
    /// </summary>
    public class CatTipoDocumentoIdentidad
    {
        public int Id { get; set; }

        /// <summary>Código numérico del tipo de documento (1..N según catálogo SIN vigente).</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial devuelta por el SIN (ej. "CI - CEDULA DE IDENTIDAD").</summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Marca de cuándo fue sincronizado por última vez.</summary>
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}