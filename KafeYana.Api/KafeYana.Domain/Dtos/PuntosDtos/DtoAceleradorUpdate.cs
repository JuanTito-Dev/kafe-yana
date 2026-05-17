using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.PuntosDtos
{
    public class DtoAceleradorUpdate
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; }

        [Required]
        public bool Activo { get; set; }

        public TimeOnly? HoraInicio { get; set; }

        public TimeOnly? HoraFin { get; set; }
    }
}
