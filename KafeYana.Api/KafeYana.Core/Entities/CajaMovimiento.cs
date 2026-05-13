using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class CajaMovimiento : BaseEntity
    {
        public required int Id_Caja { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public required string Tipo { get; set; }

        public required string Categoria { get; set; }

        public required string Descripcion { get; set; }

        public string Referencia { get; set; } = string.Empty;

        public required decimal Monto { get; set; }

        public string Nota { get; set; } = string.Empty;
        public Caja? Caja { get; set; }
    }
}
