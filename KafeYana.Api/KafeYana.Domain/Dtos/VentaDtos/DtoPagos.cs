using System.ComponentModel.DataAnnotations;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Application.Dtos.VentaDtos
{
    /// <summary>
    /// Pagos de una venta: lista de líneas (método + monto).
    ///
    /// Reemplaza la estructura fija anterior (Efectivo + Tarjeta + Qr) para
    /// soportar pagos mixtos y delegar la lista de métodos al catálogo SIN
    /// sincronizado (<c>CatMetodosPago</c> + <c>MetodoPagoSiatCatalogo</c>).
    ///
    /// Cada línea se valida contra el catálogo vigente (código válido Y
    /// activo por el operador).
    /// </summary>
    public class DtoPagos : IValidatableObject
    {
        /// <summary>
        /// Lista de líneas de pago. Una venta puede tener 1..N métodos.
        /// El sistema actualmente acepta un solo método al facturar contra
        /// el SIAT (se toma el de mayor monto), pero la estructura permite
        /// pagos mixtos para auditoría y futuros flujos.
        /// </summary>
        [Required(ErrorMessage = "La lista de pagos es obligatoria.")]
        [MinLength(1, ErrorMessage = "Debe especificar al menos una línea de pago.")]
        public required List<DtoPagoLinea> Lineas { get; set; }

        public decimal Total => Lineas?.Sum(l => l.Monto) ?? 0m;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Total <= 0)
            {
                yield return new ValidationResult(
                    "Debe especificar al menos un monto de pago mayor a 0.",
                    new[] { nameof(Lineas) });
                yield break;
            }

            // Cada línea contra el catálogo vigente del SIN + flag Activo del operador.
            foreach (var l in Lineas)
            {
                if (!MetodoPagoSiatCatalogo.EsValidoYActivo(l.CodigoMetodoPago))
                {
                    yield return new ValidationResult(
                        $"El código de método de pago {l.CodigoMetodoPago} no es válido o está deshabilitado. "
                        + "Consulte GET /api/catalogos/metodos-pago para ver los métodos habilitados.",
                        new[] { nameof(Lineas) });
                }
            }
        }
    }
}