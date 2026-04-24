using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Application.Dtos.ProductosDtos;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.Dtos.ImportacionDtos
{
    public class DtoImportacionProducto : DtoProductosLista
    {
        [Required]
        public required decimal ConversionABs { get; set; }

        public override Producto Crear()
        {
            var producto = base.Crear();
            producto.ConversionABs = this.ConversionABs > 0? this.ConversionABs : producto.ConversionABs;
            return producto;
        }

        public override void Actualizar(Producto producto)
        {
            base.Actualizar(producto);
            producto.ConversionABs = this.ConversionABs > 0 ? this.ConversionABs : producto.ConversionABs;
        }
    }
}
