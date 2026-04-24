using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.ClienteDtos
{
    public class DtoClienteCU
    {
        public int? Dni { get; set; }

        [Required(ErrorMessage = "Nombre requerido")]
        public required string Nombre { get; set; }

        [Required(ErrorMessage = "Celular requerido")]
        public required string Celular { get; set; }

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
