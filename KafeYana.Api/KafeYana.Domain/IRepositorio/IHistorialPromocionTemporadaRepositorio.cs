using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IHistorialPromocionTemporadaRepositorio : IGenericRepositorio<HistorialPromocionTemporada>
    {
        Task<HashSet<int>> ObtenerIdsPromocionesReclamadasAsync(int idCliente);

        Task<bool> ExisteReclamoAsync(int idCliente, int idPromocionTemporada);
    }
}
