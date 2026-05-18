using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.OrdenDeCompraDtos
{
    public class DtoOrdenCompra
    {
        [Required]
        public required int Id_Proveedor { get; set; }

        [Required]
        public required DateOnly FechaEntrega { get; set; }

        public string Nota { get; set; } = string.Empty;

        public List<DtoOrdenItemInsumo> Insumos { get; set; } = new List<DtoOrdenItemInsumo>();
        public List<DtoOrdenItemProducto> Productos { get; set; } = new List<DtoOrdenItemProducto>();
    }
}

