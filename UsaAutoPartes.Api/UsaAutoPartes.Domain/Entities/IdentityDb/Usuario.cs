using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Domain.Entities.IdentityDb
{
    public class Usuario : IdentityUser<Guid>
    {
        public required string Nombre { get; set; }

        public required string Apellido { get; set; }


        public static Usuario Created(string Email, string Nombre, string Apellido)
        {
            return new Usuario
            {
                Id = Guid.NewGuid(),
                UserName = Email,
                Email = Email,
                Nombre = Nombre,
                Apellido = Apellido
            };
        }

        public override string ToString()
        {
            return $"{Nombre}  {Apellido}";
        }
    }
}
