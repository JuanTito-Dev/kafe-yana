using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Promocion : BaseEntity
    {
        public int Producto_Id { get; set; }

        public Producto Producto { get; set; }

        public ICollection<PromocionDetalle> Detalles { get; set; } = new List<PromocionDetalle>();
    }
}
