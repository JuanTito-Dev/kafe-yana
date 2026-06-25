namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo de documentos sectoriales sincronizado desde el SIAT.
    /// El campo <see cref="CodigoClasificador"/> es el código que se envía en
    /// <c>&lt;codigoDocumentoSector&gt;</c> dentro del XML de la factura.
    /// Sincronizado vía <c>sincronizarParametricaTipoDocumentoSector</c>.
    /// </summary>
    public class CatDocumentoSector
    {
        public int Id { get; set; }

        /// <summary>Código numérico del clasificador (ej. 1 = Factura Compra-Venta, 24 = Nota Crédito-Débito).</summary>
        public int CodigoClasificador { get; set; }

        /// <summary>Descripción legible (ej. "FACTURA COMPRA-VENTA").</summary>
        public string Descripcion { get; set; } = string.Empty;

        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}