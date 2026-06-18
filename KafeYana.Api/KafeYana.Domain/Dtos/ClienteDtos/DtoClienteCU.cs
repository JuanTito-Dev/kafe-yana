using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.ClienteDtos
{
    public class DtoClienteCU
    {
        // Opcional: la entidad (Cliente.cs) lo define como int? y la BD tiene
        // índice único con filtro "Dni IS NOT NULL" — admite NULL. El POS
        // crea clientes anónimos (solo Nombre + Celular) sin C.L. registrada.
        public int? Dni { get; set; }

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
