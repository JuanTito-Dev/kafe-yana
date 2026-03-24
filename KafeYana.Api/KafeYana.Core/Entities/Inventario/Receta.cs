using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Receta : BaseEntity
    {
        public string Nota { get; set; } = string.Empty;

        public int? Id_Elaborado { get; set; }

        //Navegacion 
        public Elaborado Elaborado { get; set; }

        public ICollection<Detalle> Detalles {get; set;} = new List<Detalle>();
    }
}
