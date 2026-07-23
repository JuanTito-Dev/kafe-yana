using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.VentaDtos
{
    /// <summary>Datos fiscales mínimos para facturar una sub-venta ya cobrada (facturación "después").</summary>
    public class DtoFacturarSubVenta
    {
        public int? Id_Cliente { get; set; }

        [Required(ErrorMessage = "El código de tipo de documento es requerido.")]
        public int? CodigoTipoDocumento { get; set; }

        public string? Nombre { get; set; }

        public int? Dni { get; set; }

        [MaxLength(10, ErrorMessage = "El complemento no puede exceder 10 caracteres.")]
        public string? Complemento { get; set; }

        public int? CodigoSucursal { get; set; }

        public int? CodigoPuntoVenta { get; set; }

        public int? CodigoPaisOrigen { get; set; }
    }
}
