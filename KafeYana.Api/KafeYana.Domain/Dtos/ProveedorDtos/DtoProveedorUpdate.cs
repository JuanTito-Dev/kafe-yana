using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.ProveedorDtos
{
    public class DtoProveedorUpdate
    {
        [Required(ErrorMessage = "El campo Id_Proveedor es obligatorio.")]
        public required string Razon_Social { get; set; }

        public string Dni { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo Telefono es obligatorio.")]
        public string Telefono { get; set; } = string.Empty;

        public string Celular { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Direccion { get; set; } = string.Empty;
    }
}