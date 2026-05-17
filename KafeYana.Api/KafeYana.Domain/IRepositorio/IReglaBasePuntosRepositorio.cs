using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IReglaBasePuntosRepositorio : IGenericRepositorio<ReglaBasePuntos>
    {
        Task<ReglaBasePuntos?> ObtenerReglaAsync();
    }
}
