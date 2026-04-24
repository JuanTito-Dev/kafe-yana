using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.RecetaDtos
{
    public class DtoDetalle
    {
        [Required(ErrorMessage = "Cantidad requerida")]
        [Range(0, double.MaxValue, ErrorMessage = "Rango de cantidad no aceptado")]
        public decimal Cantidad { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Rango de merma no aceptado")]
        public decimal Merma { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Rango de subtotal no aceptado")]
        public decimal SubTotal { get; set; }

        [Required(ErrorMessage = "se nesecita agregar el insumo")]
        public required int Id_insumo { get; set; }
    }
}
