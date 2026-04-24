using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Exceptions
{
    public class CampoYaExistenteFailException(string campo) : Exception($"{campo} ya existe en el sistema");
}
