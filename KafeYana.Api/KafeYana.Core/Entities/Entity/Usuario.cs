using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Core.Entities.Entity
{
    public class Usuario : IdentityUser<Guid>
    {
        public required string Nombre { get; set; }

        public required string Apellido { get; set; }


        public static Usuario Crear(string email, string nombre, string Apellido, string Numero)
        {
            return new Usuario
            {
                Email = email,
                UserName = email,
                Nombre = nombre,
                Apellido = Apellido,
                PhoneNumber = Numero
            };
        }

        public override string ToString()
        {
            return Nombre + " " + Apellido;
        }
    }
}
