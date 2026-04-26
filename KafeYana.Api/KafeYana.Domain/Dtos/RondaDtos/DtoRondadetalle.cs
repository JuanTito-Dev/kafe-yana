using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace KafeYana.Domain.Dtos.RondaDtos
{
    public class DtoRondadetalle
    {
        [Required(ErrorMessage = "El campo Id_Producto es obligatorio.")]
        public int Id_Producto { get; set; }

        public List<int>? Ids_Opcion { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
        public int Cantidad { get; set; }
    }
}