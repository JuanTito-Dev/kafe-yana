using System.ComponentModel.DataAnnotations;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Application.Dtos.VentaDtos
{
    public class DtoVentaPedido : IValidatableObject
    {
        [Required]
        public required int Id_Pedido { get; set; }

        /// <summary>Cliente registrado. Si no se envía, son obligatorios Nombre y Dni (C.L.) al facturar.</summary>
        public int? Id_Cliente { get; set; }

        [Required]
        public required DtoPagos Pagos { get; set; }

        /// <summary>Si es true, aplica el mejor descuento permanente disponible. Default: false.</summary>
        public bool AplicarDescuentos { get; set; } = false;

        /// <summary>
        /// Si es true, genera factura electrónica y envía al SIAT.
        /// Si es false, solo registra el cobro sin facturación tributaria.
        /// </summary>
        public bool Factura { get; set; } = true;

        /// <summary>Paramétrica SIAT codigoTipoDocumentoIdentidad (1 a 5). Obligatorio solo si Factura=true.</summary>
        [Range(1, 5, ErrorMessage = "El código de tipo de documento debe estar entre 1 y 5.")]
        public int? CodigoTipoDocumento { get; set; }

        /// <summary>Nombre del comprador. Obligatorio solo si no se envía Id_Cliente al facturar.</summary>
        public string? Nombre { get; set; }

        /// <summary>Cédula de identidad (C.L.). Obligatoria solo si no se envía Id_Cliente al facturar.</summary>
        public int? Dni { get; set; }

        /// <summary>Complemento SEGIP. Opcional; null si no aplica.</summary>
        [MaxLength(10, ErrorMessage = "El complemento no puede exceder 10 caracteres.")]
        public string? Complemento { get; set; }

        /// <summary>
        /// Sucursal donde se realiza el cobro, declarada en PuntosVentaSiat.
        /// Si se envía (junto con CodigoPuntoVenta), el backend valida que el
        /// (sucursal, puntoVenta) exista y esté activo, y lo usa para construir
        /// el CUF/CUFD/sobre SOAP de forma consistente.
        /// Si NO se envía, el backend cae al comportamiento legacy:
        /// primer PuntosVentaSiat activo, o appsettings si no hay ninguno.
        /// </summary>
        public int? CodigoSucursal { get; set; }

        /// <summary>
        /// Punto de venta donde se realiza el cobro, declarado en PuntosVentaSiat.
        /// Mismas reglas que <see cref="CodigoSucursal"/>: si viene del frontend
        /// se valida contra BD; si no, fallback automático.
        /// </summary>
        public int? CodigoPuntoVenta { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Factura)
            {
                if (CodigoTipoDocumento is null)
                {
                    yield return new ValidationResult(
                        "El código de tipo de documento es requerido cuando Factura es true.",
                        [nameof(CodigoTipoDocumento)]);
                }
                else if (!Enum.IsDefined(typeof(TipoDocumentoIdentidadSiat), CodigoTipoDocumento.Value))
                {
                    yield return new ValidationResult(
                        "El código de tipo de documento no es válido. Valores permitidos: 1 (CI), 2 (CEX), 3 (PAS), 4 (OD), 5 (NIT).",
                        [nameof(CodigoTipoDocumento)]);
                }

                if (Id_Cliente is int idCliente)
                {
                    if (idCliente <= 0)
                    {
                        yield return new ValidationResult(
                            "Id_Cliente debe ser mayor a cero.",
                            [nameof(Id_Cliente)]);
                    }

                    yield break;
                }

                if (!string.IsNullOrWhiteSpace(Nombre) && Dni is > 0)
                    yield break;
            }

            // CodigoSucursal y CodigoPuntoVenta deben venir juntos o ninguno.
            // Si viene solo uno, es un error del frontend (no adivinar el resto).
            if ((CodigoSucursal is null) != (CodigoPuntoVenta is null))
            {
                yield return new ValidationResult(
                    "Debe enviar CodigoSucursal y CodigoPuntoVenta juntos, o ninguno.",
                    [nameof(CodigoSucursal), nameof(CodigoPuntoVenta)]);
            }

            // Si vienen, validar rangos razonables (BD check es en VentaServices).
            if (CodigoSucursal is int suc && suc < 0)
            {
                yield return new ValidationResult(
                    "CodigoSucursal no puede ser negativo.",
                    [nameof(CodigoSucursal)]);
            }

            if (CodigoPuntoVenta is int pv && pv < 0)
            {
                yield return new ValidationResult(
                    "CodigoPuntoVenta no puede ser negativo.",
                    [nameof(CodigoPuntoVenta)]);
            }
            else if (CodigoTipoDocumento is int tipo
                     && !Enum.IsDefined(typeof(TipoDocumentoIdentidadSiat), tipo))
            {
                yield return new ValidationResult(
                    "El código de tipo de documento no es válido. Valores permitidos: 1 (CI), 2 (CEX), 3 (PAS), 4 (OD), 5 (NIT).",
                    [nameof(CodigoTipoDocumento)]);
            }
        }
    }
}
