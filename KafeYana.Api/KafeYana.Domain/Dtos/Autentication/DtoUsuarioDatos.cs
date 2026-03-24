using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.Autentication
{
    public class DtoUsuarioDatos
    {
        public string Nombre {  get; set; }

        public string Apellido { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string Celular { get; set; }

        public bool  Estado { get; set; }

    }
}
