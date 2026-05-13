using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.OrdenDeCompraDtos
{
    public class DtoOrdenItemInsumo
    {
        [Required]
        public required int Id_Insumo { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public required int Cantidad { get; set; }
        [Required]
        [Range(0.01, double.MaxValue)]
        public required decimal Precio { get; set; }
    }
}
