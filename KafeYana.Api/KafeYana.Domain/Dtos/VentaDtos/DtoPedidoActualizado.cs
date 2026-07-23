using System.Text.Json.Serialization;

namespace KafeYana.Application.Dtos.VentaDtos
{
    public class DtoPedidoActualizado
    {
        [JsonPropertyName("id_Pedido")]
        public int Id_Pedido { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("totalAbonado")]
        public decimal TotalAbonado { get; set; }

        [JsonPropertyName("saldo")]
        public decimal Saldo { get; set; }

        [JsonPropertyName("abonos")]
        public List<DtoAbonoResumen> Abonos { get; set; } = [];

        [JsonPropertyName("detalles")]
        public List<DtoDetalleEstado> Detalles { get; set; } = [];
    }

    public class DtoAbonoResumen
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        [JsonPropertyName("fecha")]
        public DateTime Fecha { get; set; }

        [JsonPropertyName("codigoMetodoPago")]
        public int CodigoMetodoPago { get; set; }

        [JsonPropertyName("esPagoFinal")]
        public bool EsPagoFinal { get; set; }

        [JsonPropertyName("facturada")]
        public bool Facturada { get; set; }

        [JsonPropertyName("idVenta")]
        public int? IdVenta { get; set; }

        [JsonPropertyName("itemsCubiertos")]
        public List<object> ItemsCubiertos { get; set; } = [];

        [JsonPropertyName("pagos")]
        public List<object> Pagos { get; set; } = [];

        [JsonPropertyName("vendedorId")]
        public string VendedorId { get; set; } = string.Empty;

        [JsonPropertyName("pedidoId")]
        public int PedidoId { get; set; }
    }

    public class DtoDetalleEstado
    {
        [JsonPropertyName("id_Detalle")]
        public int Id_Detalle { get; set; }

        [JsonPropertyName("id_Producto")]
        public int Id_Producto { get; set; }

        [JsonPropertyName("cantidadDescontada")]
        public int CantidadDescontada { get; set; }
    }
}
