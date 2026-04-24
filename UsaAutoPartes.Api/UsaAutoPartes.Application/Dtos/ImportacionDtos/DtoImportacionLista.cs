using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Application.Dtos.ImportacionDtos
{
    public class DtoImportacionLista
    {
        [Required]
        public required int Id_Proveedor { get; set; }

        [Required]
        public required DateTime Fecha { get; set; }

        [Required]
        public required decimal CostoTotal {  get; set; }

        public List<DtoImportacionProducto> Productos { get; set; }
    }
}
