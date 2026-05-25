using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Domain.Entities
{
    public class Venta : BaseEntity
    {
        public string Codigo { get; set; }

        public DateTime Fecha { get; set; }

        public required string Cliente { get; set; }

        public int? Id_Cliente { get; set; }

        public required string Cajero { get; set; }

        public required int Productos { get; set; } = 0;

        public decimal PagoEfectivo { get; set; } = 0;

        public decimal PagoTarjeta { get; set; } = 0;

        public decimal PagoQr { get; set; } = 0;

        public required string Estado { get; set; }

        /// <summary>Total del pedido antes de descuento por promoción permanente.</summary>
        public required decimal Subtotal { get; set; }

        public decimal MontoDescuento { get; set; } = 0;

        public int? PorcentajeDescuento { get; set; }

        public int? Id_PromocionPermanenteDescuento { get; set; }

        public string? NombrePromocionDescuento { get; set; }

        /// <summary>Total cobrado (Subtotal - MontoDescuento).</summary>
        public required decimal Total { get; set; }

        public List<Detalle_venta> Detalles { get; set; } = new List<Detalle_venta>();

        public CajaMovimiento Reembolso(Caja caja, decimal monto, TipoPagos tipoPago, string motivo)
        {
            Estado = "Reembolsado";
            return caja.RegistrarReembolso(monto, tipoPago, motivo, Codigo);
        }
    }
}
