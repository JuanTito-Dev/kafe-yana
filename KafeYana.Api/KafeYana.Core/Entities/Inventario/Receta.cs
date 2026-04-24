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
        public required string Nombre { get; set; } 

        public required int Porciones { get; set; }
        public string Nota { get; set; } = string.Empty;

        public int? Id_Elaborado { get; set; }

        //Navegacion 
        public Elaborado Elaborado { get; set; }

        public ICollection<Detalle> Detalles {get; set;} = new List<Detalle>();
    }
}
