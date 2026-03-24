using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Elaborado : BaseEntity
    {
        public required string Unidad_medida { get; set; }
        
        public required int Id_Producto { get; set; }

        //Navegacion
        public Producto Producto { get; set; }

        public Receta Receta { get; set; }

        public ICollection<Variacion> Variaciones { get; set; } = new List<Variacion>();
    }
}
