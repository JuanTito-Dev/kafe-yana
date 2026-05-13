using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.Autentication
{
    public class DtoUsuarioDatoU
    {
        [Required]
        public string nombre { get; set; }
        [Required]
        public string apellido { get; set; }

        [Required]
        [EmailAddress]
        public string email {  get; set; }

        [Required]
        public string telefono {  get; set; }
    }
}
