using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class InsumoMovimiento : BaseMovimientos
    {
        public int Id_insumo { get; private set; }
        
        public Insumo? Insumo { get; set; }

        public InsumoMovimiento() { }

        public InsumoMovimiento(int Id_Inusmo, string Tipo, string referencia, int cantidad, decimal costo, int stock) :
            base(Tipo, referencia, cantidad, costo, stock)
        {
            this.Id_insumo = Id_Inusmo;
        }
    }
}
