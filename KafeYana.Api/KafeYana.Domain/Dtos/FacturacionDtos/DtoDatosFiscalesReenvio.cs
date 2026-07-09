using System.ComponentModel.DataAnnotations;
using KafeYana.Domain.TiposDeDatos;

namespace KafeYana.Application.Dtos.FacturacionDtos
{
    /// <summary>
    /// Datos fiscales opcionales para actualizar una Venta nunca facturada
    /// antes de reenviarla al SIAT (ver <c>FacturacionController.ReenviarFactura</c>).
    /// Todo es opcional: null/vacío = conservar los datos ya grabados en la Venta.
    /// </summary>
    public class DtoDatosFiscalesReenvio : IValidatableObject
    {
        public int? Id_Cliente { get; set; }

        public int? CodigoTipoDocumento { get; set; }

        public string? Nombre { get; set; }

        public int? Dni { get; set; }

        [MaxLength(10, ErrorMessage = "El complemento no puede exceder 10 caracteres.")]
        public string? Complemento { get; set; }

        public int? CodigoPaisOrigen { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Id_Cliente is int idCliente && idCliente <= 0)
            {
                yield return new ValidationResult(
                    "Id_Cliente debe ser mayor a cero.",
                    [nameof(Id_Cliente)]);
            }

            if (string.IsNullOrWhiteSpace(Nombre) != (Dni is null))
            {
                yield return new ValidationResult(
                    "Nombre y Dni deben enviarse juntos, o ninguno.",
                    [nameof(Nombre), nameof(Dni)]);
            }

            if (CodigoTipoDocumento is int tipo && !TipoDocumentoIdentidadSiatCatalogo.EsValido(tipo))
            {
                yield return new ValidationResult(
                    "El código de tipo de documento no es válido. Valores permitidos según catálogo SIAT vigente (1=CI, 2=CEX, 3=PAS, 4=OD, 5=NIT).",
                    [nameof(CodigoTipoDocumento)]);
            }

            var esExtranjero = CodigoTipoDocumento is 2 or 3;
            if (esExtranjero && Id_Cliente is null && CodigoPaisOrigen is null)
            {
                yield return new ValidationResult(
                    "Debe indicar el país de origen del documento para clientes extranjeros (CEX/PAS).",
                    [nameof(CodigoPaisOrigen)]);
            }
        }
    }
}
