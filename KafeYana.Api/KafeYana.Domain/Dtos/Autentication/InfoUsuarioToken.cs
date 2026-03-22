using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.Autentication
{
    public record InfoUsuarioToken
    {
        public required Guid Id { get; set; }

        public required string Email { get; set; } = string.Empty;

        public required string Nombre { get; set; } = string.Empty;

        public required string Rol { get; set; } = string.Empty;
    }
}
