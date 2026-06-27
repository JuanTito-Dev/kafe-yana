using System.ComponentModel.DataAnnotations;

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
        /// <summary>Código SIN del método de pago (catálogo CatMetodosPago, 1..308).</summary>
        [Required(ErrorMessage = "El código de método de pago es obligatorio.")]
        public int CodigoMetodoPago { get; set; }

        /// <summary>Monto pagado con este método en Bolivianos.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "El monto del pago no puede ser negativo.")]
        public decimal Monto { get; set; } = 0;
    }
}