using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Exceptions.Usuarios
{
    public class RegiterUsuarioFailException(IEnumerable<string> exception) : 
        Exception($"Error al crear el usuario {string.Join(Environment.NewLine, exception)}")
    {
    }
}
