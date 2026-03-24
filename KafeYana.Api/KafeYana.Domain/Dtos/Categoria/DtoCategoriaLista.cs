using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.Categoria
{
    public class DtoCategoriaLista
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public string Color { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }
}
