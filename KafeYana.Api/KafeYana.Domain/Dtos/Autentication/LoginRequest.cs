using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Request
{
    public record LoginRequest
    {
        [Required(ErrorMessage = "Email Requerido")]
        [EmailAddress]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Contraseña requerida")]
        [PasswordPropertyText]
        public required string Password { get; set; }
    }
}
