using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IHistorialHitoCompraRepositorio : IGenericRepositorio<HistorialHitoCompra>
    {
        Task<bool> ExisteReclamoAsync(int idCliente, int idHitoCompra);

        Task<List<HistorialHitoCompra>> ObtenerReclamadosPorClienteAsync(int idCliente);
    }
}
