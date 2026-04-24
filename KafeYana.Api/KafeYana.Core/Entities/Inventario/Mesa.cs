using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Mesa : BaseEntity
    {
        public required string Nombre { get; set; }

        public int? Id_Pedido { get; set; }

        public bool Disponible { get; set; } = true;

        public Pedido pedido { get; set; }
    }
}
