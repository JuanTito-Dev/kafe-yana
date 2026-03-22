using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Exceptions.Usuarios
{
    public class RefreshTokenExceptions(string mesaje) : Exception($"{mesaje}");
}
