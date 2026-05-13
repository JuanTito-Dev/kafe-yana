using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.CajaDtos
{
    public class DtoCerrarCaja
    {
        [Required]
        public required decimal MontoFinal { get; set; }
        public string Nota { get; set; } = string.Empty;
    }
}
