using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class CajaHistorialMovimiento : BaseEntity
    {
        public  int Id_CajaHistorial { get; set; }
        public required string Codigo { get; set; }
        public required string Categoria { get; set; }
        public required string Tipo { get; set; }
        public required string Descripcion { get; set; }
        public required decimal Monto { get; set; }
        public CajaHistorial? CajaHistorial { get; set; }
    }
}
