using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KafeYana.Application.Dtos.VentaDtos
{
    /// <summary>
    /// Una línea de pago individual de una venta: un método de pago SIN + su monto.
    ///
    /// Una venta puede tener N líneas (pago mixto: ej. efectivo + transferencia).
    /// El código <c>CodigoMetodoPago</c> se valida contra
    /// <c>MetodoPagoSiatCatalogo.EsValidoYActivo</c> en <c>DtoPagos.Validate</c>.
    /// </summary>
    public class DtoPagoLinea
    {
        /// <summary>
        /// Código SIN del método de pago (catálogo CatMetodosPago, 1..308).
        ///
        /// El JSON usa la clave corta <c>"codigo"</c> (no <c>"CodigoMetodoPago"</c>) — el
        /// frontend serializa el body del cobro con <c>{lineas:[{codigo, monto}]}</c>
        /// desde <c>POSPage.tsx</c> y <c>DividirCuentaPanel.tsx</c>. Sin este
        /// <c>JsonPropertyName</c>, <c>System.Text.Json</c> no encuentra match
        /// (son nombres distintos, no sólo distinto case, y <c>Program.cs</c> tiene
        /// <c>PropertyNamingPolicy = null</c>) → la propiedad queda en su default
        /// <c>int = 0</c> → <c>EsValidoYActivo(0)</c> devuelve <c>false</c> y
        /// <c>DtoPagos.Validate</c> rechaza el cobro con
        /// "El código de método de pago 0 no es válido".
        /// </summary>
        [Required(ErrorMessage = "El código de método de pago es obligatorio.")]
        [JsonPropertyName("codigo")]
        public int CodigoMetodoPago { get; set; }

        /// <summary>Monto pagado con este método en Bolivianos.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "El monto del pago no puede ser negativo.")]
        public decimal Monto { get; set; } = 0;
    }
}