using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>
    /// Detalle (línea) de una NotaAjuste. El XSD exige referenciar la línea original de
    /// la factura que se ajusta: por eso IdDetallePagoOriginal y NumeroLineaOriginal son obligatorios.
    /// </summary>
    public class NotaAjusteDetalle : BaseEntity
    {
        // --- FK a la nota ---

        public int IdNotaAjuste { get; set; }

        public NotaAjuste? NotaAjuste { get; set; }

        // --- Campos espejo de Detalle_Pago (mismas reglas SIAT) ---

        public required string ActividadEconomica { get; set; }

        public int CodigoProductoSin { get; set; }

        public required string CodigoProducto { get; set; }

        public required string Descripcion { get; set; }

        public decimal Cantidad { get; set; }

        public int UnidadMedida { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal SubTotal { get; set; }

        public decimal? MontoDescuento { get; set; }

        // --- Campos específicos de nota (obligatorios por XSD) ---

        /// <summary>Tipo de transacción de la línea (1=Devolución, 2=Descuento, etc.).</summary>
        public int CodigoDetalleTransaccion { get; set; }

        /// <summary>FK a la línea de Detalle_Pago original que esta nota está ajustando.</summary>
        public int IdDetallePagoOriginal { get; set; }

        /// <summary>Número de línea de la factura original (1, 2, 3, ...).</summary>
        public int NumeroLineaOriginal { get; set; }

        /// <summary>
        /// Número correlativo de ítem dentro de la nota (1, 2, 3, ...).
        /// Obligatorio SOLO para sector 47 (XSD <c>notaComputarizadaCreditoDebitoDescuento.xsd</c>
        /// — primer hijo de <c>&lt;detalle&gt;</c>). Para sector 24 se mantiene en 0
        /// (no se serializa). El preparer lo setea correlativo antes de generar el XML.
        /// </summary>
        public int NroItem { get; set; }
    }
}
