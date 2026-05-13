using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class Detalle_Ronda_Opcion
    {
        public int Id_Detalle_Ronda { get; set; }
        public int Id_Opcion { get; set; }
        public Detalle_ronda? Detalle_Ronda { get; set; }
        public Opcion? Opcion { get; set; }
    }
}
