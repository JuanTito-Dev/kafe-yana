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
        [RegularExpression(@"^[a-zA-Z0-9._-]{3,20}$", ErrorMessage = "Usuario: 3-20 caracteres, solo letras, números, puntos, guiones y guiones bajos")]
        public string usuario {  get; set; }

        [Required]
        public string telefono {  get; set; }
    }
}
