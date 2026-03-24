using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.ProductoDtos
{
    public class DtoProductosAll : BaseEntity
    {
        public string Nombre { get; set; }

        public string Tipo { get; set; }

        public string CategoriaNombre { get; set; }

        public decimal PrecioVenta { get; set; }

        public decimal Costo { get; set; }

        public int Stock {  get; set; }

        public string RecetaName { get; set; }

    }
}
