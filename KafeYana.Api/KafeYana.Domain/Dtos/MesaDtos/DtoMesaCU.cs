using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.MesaDtos
{
    public class DtoMesaCU
    {
        [Required]
        public required string Nombre { get; set; }
    }
}
