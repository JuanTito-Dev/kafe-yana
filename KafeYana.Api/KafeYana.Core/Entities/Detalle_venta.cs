using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class Detalle_venta : BaseEntity
    {
        public int Id_venta { get; set; }

        public required string Nombre { get; set; }

        public required int Cantidad { get; set; }

        public required decimal Precio { get; set; }

        public required decimal Total { get; set; }

        public Venta? venta { get; set; }
    }
}
