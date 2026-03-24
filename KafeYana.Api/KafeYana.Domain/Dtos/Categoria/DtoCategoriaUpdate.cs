using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.Categoria
{
    public class DtoCategoriaUpdate
    {
        [Required(ErrorMessage = "Nombre Obligatorio")]
        [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres")]
        public required string Nombre { get; set; }

        [MaxLength(500, ErrorMessage = "La descripcion no puede superar 500 caracteres")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado es obligatorio")]
        public required bool Estado { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [MaxLength(7, ErrorMessage = "El color no puede superar 500 caracteres")]
        public required string Color { get; set; }
    }
}
