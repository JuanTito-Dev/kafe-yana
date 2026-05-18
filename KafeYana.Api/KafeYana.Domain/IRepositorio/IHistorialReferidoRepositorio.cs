using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IHistorialReferidoRepositorio : IGenericRepositorio<HistorialReferido>
    {
        IQueryable<HistorialReferido> GetHistorial();
    }
}
