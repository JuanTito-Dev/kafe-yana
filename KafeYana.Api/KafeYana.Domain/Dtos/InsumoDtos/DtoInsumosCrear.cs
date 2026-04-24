using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.Insumos
{
    public class DtoInsumosCrear
    {
        [Required(ErrorMessage = "Nombre obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nombre de categorioa obligatorio")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unidad minima requerida")]
        public string Unidad_min_uso { get; set; } = string.Empty;

        [Required( ErrorMessage = "Unidad de compra requerida")]
        public string Unidad_compra { get; set; } = string.Empty;

        [Required (ErrorMessage = "Factor conversion requerida")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Rango no permitido")]
        public decimal Factor_conversion { get; set; }

        [Required(ErrorMessage = "Costo requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Rango de precio no permitido")]
        public required decimal Costo { get; set; }

        [Required(ErrorMessage = "Stock requerido")]
        [Range(0, int.MaxValue, ErrorMessage = "Rango no permitido")]
        public int Stock_actual { get; set; }

        [Required(ErrorMessage = "Stock alerta requerido")]
        [Range(0, int.MaxValue, ErrorMessage = "Rango no permitido")]
        public int Stock_min { get; set; }
    }
}
