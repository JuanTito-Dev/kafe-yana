using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Application.Dtos.ProductosDtos
{
    public class ProductoCU
    {
        [Required(ErrorMessage = "Codigo requerido")]
        public required string Codigo { get; set; }

        public string CodigoAux { get; set; } = string.Empty;

        public string CodigoAux2 { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nombre requerido")]
        public required string Nombre { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string Unidad_Medida { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Stock Actual requerido")]
        public required int Stock_Actual { get; set; }
        [Required(ErrorMessage = "Stock Minimo requerido")]
        public required int Stock_Minimo { get; set; }

        [Required(ErrorMessage = "Costo requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Costo debe ser mayor a 0")]
        public required decimal Costo { get; set; }

        [Required(ErrorMessage = "Precio requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Precio debe ser mayor a 0")]
        public required decimal Precio { get; set; }

        [Required(ErrorMessage = "ConversionABs requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "ConversionABs debe ser mayor a 0")]
        public required decimal ConversionABs { get; set; }
    }
}
