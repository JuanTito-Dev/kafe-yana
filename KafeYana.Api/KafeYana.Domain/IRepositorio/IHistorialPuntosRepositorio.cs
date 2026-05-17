using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IHistorialPuntosRepositorio : IGenericRepositorio<HistorialPuntos>
    {
        Task<List<HistorialPuntos>> ObtenerPorClienteAsync(int idCliente);
    }
}
