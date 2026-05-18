using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IReferidosConfigRepositorio : IGenericRepositorio<ReferidosConfig>
    {
        Task<ReferidosConfig?> ObtenerUnicaAsync();
    }
}
