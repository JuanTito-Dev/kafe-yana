using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Request
{
    public record RegisterRequest
    {
        [Required(ErrorMessage = "Nombre obligatorio")]
        public required string Nombre { get; set; }

        [Required(ErrorMessage = "Apellido obligatorio")]
        public required string Apellido { get; set; }

        [Required(ErrorMessage = "Email obligatorio")]
        [EmailAddress]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Contraseña obligatoria")]
        [PasswordPropertyText]
        public required string Password { get; set; }
        public required string NumeroPhone { get; set; }
    }
}
