using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Domain.Entities
{
    public  class Producto : BaseEntity 
    {
        public required string Codigo { get; set; }

        public string CodigoAux { get; set; } = string.Empty;

        public string CodigoAux2 { get; set; } = string.Empty;

        public required string Nombre { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string Unidad_Medida { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;

        public required int Stock_Actual { get; set; }

        public required int Stock_Minimo { get; set; }

        public required decimal Costo { get; set; }

        public required decimal Precio { get; set; }

        public required decimal ConversionABs { get; set; }
    }
}
