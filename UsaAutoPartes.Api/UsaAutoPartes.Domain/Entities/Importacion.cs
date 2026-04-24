using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Domain.Entities
{
    public class Importacion : BaseEntity
    {
        public string Codigo { get; set; }
        
        public required int Id_Proveedor {  get; set; }

        public required DateTime Fecha { get; set; }

        public required int CantProductos { get; set; }

        public decimal Total { get; set; } = 0.00M;

        public string Estado { get; set; } = "Recibida";

        public Proveedor Proveedor { get; set; } 
    }
}
