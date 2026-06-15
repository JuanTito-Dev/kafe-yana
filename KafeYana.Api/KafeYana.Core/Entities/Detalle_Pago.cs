using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>Detalle de Factura Compra y Venta (SIAT).</summary>
    public class Detalle_Pago : BaseEntity
    {
        public int Id_venta { get; set; }

        // --- Obligatorios ---

        public required string ActividadEconomica { get; set; }

        public int CodigoProductoSin { get; set; }

        public required string CodigoProducto { get; set; }

        public required string Descripcion { get; set; }

        public decimal Cantidad { get; set; }

        public int UnidadMedida { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal SubTotal { get; set; }

        // --- Opcionales ---

        public decimal? MontoDescuento { get; set; }

        public string? NumeroSerie { get; set; }

        public string? NumeroImei { get; set; }

        public Venta? Venta { get; set; }
    }
}
