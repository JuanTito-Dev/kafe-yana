using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Ronda : BaseEntity
    {
        public required int Id_Pedido { get; set; }

        public required string Ronda_Descripcion { get; set; }

        public required decimal SubTotal { get; set; } = 0.00M;

        public Pedido? pedido { get; set; }
        public List<Detalle_ronda> Detalle { get; set; } = new List<Detalle_ronda>();
    }
}
