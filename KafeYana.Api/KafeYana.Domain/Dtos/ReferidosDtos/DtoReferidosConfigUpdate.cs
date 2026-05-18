using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.ReferidosDtos
{
    public class DtoReferidosConfigUpdate
    {
        [Required]
        [Range(0, int.MaxValue)]
        public int PuntosReferidor { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int PuntosReferido { get; set; }

        [Required]
        public bool Activo { get; set; }
    }
}
