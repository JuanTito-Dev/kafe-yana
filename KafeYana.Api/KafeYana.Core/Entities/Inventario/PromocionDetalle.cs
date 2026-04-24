using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class PromocionDetalle
    {
        public required int Cantidad {  get; set; }

        public required bool Opcional { get; set; }

        public int Id_Promocion    { get; set; }

        public required int Id_Producto { get; set; }

        public Promocion Promocion { get; set; }

        public Producto Producto { get; set; }
    }
}
