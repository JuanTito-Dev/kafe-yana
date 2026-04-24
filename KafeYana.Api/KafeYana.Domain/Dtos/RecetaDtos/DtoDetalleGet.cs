using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.RecetaDtos
{
    public class DtoDetalleGet
    {
        public decimal Cantidad { get; set; }
        public decimal Merma { get; set; }
        public decimal SubTotal { get; set; }
        public required int Id_insumo { get; set; }

        public string Nombre { get; set; } = string.Empty;  
    }
}
