using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KafeYana.Domain.Entities.BaseEntidades;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Detalle_Ronda_Opcion
    {
        public int Id_Detalle_Ronda { get; set; }

        public int Id_Opcion { get; set; }

        public Detalle_ronda? Detalle_Ronda { get; set; }

        public Opcion? Opcion { get; set; }
    }
}
