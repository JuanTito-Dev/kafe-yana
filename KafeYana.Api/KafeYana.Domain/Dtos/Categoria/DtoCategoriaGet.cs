using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.Categoria
{
    public class DtoCategoriaGet
    {
        public required string Nombre { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public required bool Estado { get; set; }

        public required string Color { get; set; }
    }
}
