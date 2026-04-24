using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Opcion : BaseEntity
    {
        public required string Nombre { get; set; }

        public required decimal AjustePrecio { get; set; }

        public string TipoOpcion { get; set; } = "normal";

        public string? ValorAnterior { get; set; }

        public int Id_variacion { get; set; }

        public Variacion Variacion { get; set; }

        public ICollection<Ajuste> Ajustes { get; set; } = new List<Ajuste>();


    }
}
