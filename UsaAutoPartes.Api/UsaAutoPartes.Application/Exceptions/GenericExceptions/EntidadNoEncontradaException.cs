using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Application.Exceptions.GenericExceptions
{
    public class EntidadNoEncontradaException() : Exception($"La entidad no existe."); 
}
