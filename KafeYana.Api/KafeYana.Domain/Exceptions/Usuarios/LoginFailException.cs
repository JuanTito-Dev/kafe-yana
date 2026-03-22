using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Exceptions.Usuarios
{
    public class LoginFailException(string email) : Exception($"Credenciales incorrectas {email}");
}
