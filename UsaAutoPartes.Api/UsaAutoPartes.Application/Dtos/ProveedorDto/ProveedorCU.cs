using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Application.Dtos.ProveedorDto
{
    public class ProveedorCU
    {
        [Required(ErrorMessage = "Nombre obligatorio")]
        public required string Nombre { get; set; }

        [Required(ErrorMessage = "Pais obligatorio")]
        public required string Pais { get; set; }

        [Required(ErrorMessage = "Moneda Obligatoria")]
        public required string Moneda { get; set; }

        [Required]
        public required string Terminos { get; set; }

        [Required]
        public required string Nombre_Contacto { get; set; }

        [EmailAddress]
        [Required]
        public required string Email { get; set; }

        public string Telefono { get; set; } = string.Empty;

        public int TiempoReposicion { get; set; } = 0;

        public string SitioWeb { get; set; } = string.Empty;

        public bool Estado { get; set; } = true;

        public string Nota { get; set; } = string.Empty;
    }
}
