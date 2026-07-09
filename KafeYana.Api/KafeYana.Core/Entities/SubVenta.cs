using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities
{
    /// <summary>Cobro independiente de una porción de lo consumido en un pedido (mesa o para llevar).</summary>
    public class SubVenta : BaseEntity
    {
        public int Id_Pedido { get; set; }
        public Pedido? Pedido { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public int CodigoMetodoPago { get; set; }

        /// <summary>True si este cobro dejó el pedido entero sin cantidad pendiente.</summary>
        public bool EsPagoFinal { get; set; }

        public string Cajero { get; set; } = string.Empty;

        /// <summary>True una vez que tiene factura emitida (inmutable a partir de entonces).</summary>
        public bool Facturada { get; set; } = false;

        public int? Id_Venta { get; set; }
        public Venta? Venta { get; set; }

        public List<SubVentaDetalle> Detalles { get; set; } = [];
    }
}
