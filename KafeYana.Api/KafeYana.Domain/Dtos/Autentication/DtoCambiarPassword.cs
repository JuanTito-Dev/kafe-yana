using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.Autentication
{
    public class DtoCambiarPassword
    {
        [Required]
        public required string PasswordActual { get; set; }
        [Required]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public required string PasswordNueva { get; set; }
    }
}
