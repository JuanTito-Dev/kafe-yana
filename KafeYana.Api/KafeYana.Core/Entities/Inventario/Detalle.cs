using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Detalle
    {
        public required decimal Cantidad { get; set; }

        public required decimal Merma { get; set; }

        public required decimal SubTotal { get; set; }

        public int Id_insumo { get; set; }

        public int Id_receta { get; set; }

        //Navegacion 
        public Receta Receta { get; set; }

        public Insumo Insumo { get; set; }

    }
}
