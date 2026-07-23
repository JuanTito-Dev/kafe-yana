using System.Text.Json.Serialization;

namespace KafeYana.Application.Dtos.VentaDtos
{
    /// <summary>
    /// Fila de un listado de sub-ventas. Se usa tanto para "pendientes de
    /// facturar" (<c>GET /SubVenta/pendientes</c>, solo no facturadas) como
    /// para el historial completo de un pedido (<c>GET /SubVenta/pedido/{id}</c>,
    /// facturadas y no) — el frontend necesita ambas vistas para no depender
    /// de estado local de sesión.
    /// </summary>
    public class DtoSubVentaPendiente
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fecha")]
        public DateTime Fecha { get; set; }

        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        [JsonPropertyName("pedidoId")]
        public int PedidoId { get; set; }

        [JsonPropertyName("origen")]
        public string Origen { get; set; } = string.Empty;

        [JsonPropertyName("cajero")]
        public string Cajero { get; set; } = string.Empty;

        [JsonPropertyName("cantidadLineas")]
        public int CantidadLineas { get; set; }

        [JsonPropertyName("facturada")]
        public bool Facturada { get; set; }

        [JsonPropertyName("esPagoFinal")]
        public bool EsPagoFinal { get; set; }

        [JsonPropertyName("idVenta")]
        public int? IdVenta { get; set; }

        [JsonPropertyName("codigoMetodoPago")]
        public int CodigoMetodoPago { get; set; }

        [JsonPropertyName("detalles")]
        public List<DtoSubVentaDetalleResumen> Detalles { get; set; } = [];
    }

    /// <summary>
    /// Copia (snapshot) de una línea cobrada por la sub-venta — exactamente lo
    /// que se guardó al momento del cobro, independiente de si la ronda de
    /// origen se editó o borró después.
    /// </summary>
    public class DtoSubVentaDetalleResumen
    {
        [JsonPropertyName("nombreProducto")]
        public string NombreProducto { get; set; } = string.Empty;

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; }

        [JsonPropertyName("precio")]
        public decimal Precio { get; set; }
    }
}
