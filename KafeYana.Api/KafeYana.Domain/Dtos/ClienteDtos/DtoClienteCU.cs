using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.ClienteDtos
{
    public class DtoClienteCU
    {
        [Required(ErrorMessage = "C.L. requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "C.L. inválido")]
        public required int Dni { get; set; }

        [Required(ErrorMessage = "Nombre requerido")]
        public required string Nombre { get; set; }

        public string? Celular { get; set; }

        [EmailAddress]
        public string? Correo { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Range(typeof(DateTime), "1/1/1900", "1/1/2100", ErrorMessage = "La fecha debe estar entre 1900 y 2100")]
        public DateTime? Fecha_nacimiento { get; set; }

        public string? Direccion { get; set; }

        public bool Estado { get; set; } = true;
    }
}
