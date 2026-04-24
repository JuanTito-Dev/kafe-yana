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

        public required bool Requerido { get; set; }

        //Fk

        public required int Id_Elaborado { get; set; }

        public Elaborado Elaborado { get; set; }

        public List<Opcion> Opciones { get; set; } = new List<Opcion>();
    }
}
