using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.CajaDtos
{
    public class DtoAbrir
    {
        [Required]
        public required decimal SaldoInicial { get; set; }
    }
}
