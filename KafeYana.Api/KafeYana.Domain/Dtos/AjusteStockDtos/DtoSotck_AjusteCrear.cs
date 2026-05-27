using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.AjusteStockDtos
{
    public class DtoSotck_AjusteCrear
    {

        [Required]
        [Range(0, maximum:int.MaxValue, ErrorMessage = "Id Incorrecto")]
        public required int Id { get; set; }

        [Range(0, maximum: int.MaxValue, ErrorMessage = "No puedes quitar más de la cantidad disponible")]
        public int Cantidad { get; set; }

        public required string Motivo { get; set; } = string.Empty;

        public string Nota { get; set; } = string.Empty;
    }
}
