using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Variacion : BaseEntity
    {
        public required string Nombre { get; set; }

        public required bool Requirido { get; set; }

        //Fk

        public required int Id_Elaborado { get; set; }

        public Elaborado Elaborado { get; set; }

        public ICollection<Opcion> Opciones { get; set; } = new List<Opcion>();
    }
}
