using System.ComponentModel.DataAnnotations;
using KafeYana.Application.Dtos.ClienteDtos;

namespace KafeYana.Application.Dtos.ReferidosDtos
{
    /// <summary>Mismos datos que crear cliente más el Id del cliente referidor.</summary>
    public class DtoClienteReferidoCU : DtoClienteCU
    {
        [Required(ErrorMessage = "IdReferidor es obligatorio")]
        [Range(1, int.MaxValue)]
        public int IdReferidor { get; set; }
    }
}
