using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.CajaDtos
{
    public class DtoCajaMovimiento
    {
        [Required]
        public string Categoria { get; set; } = string.Empty;

        [Required]
        public string Concepto { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Rango invalido")]
        public decimal Cantidad {  get; set; }

        public string Referencia { get; set; } = string.Empty;

        public string Nota { get; set; } = string.Empty;
    }
}
