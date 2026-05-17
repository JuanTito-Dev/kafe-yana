using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IAceleradorPuntosRepositorio : IGenericRepositorio<AceleradorPuntos>
    {
        Task<List<AceleradorPuntos>> ObtenerTodosAsync();

        Task<AceleradorPuntos?> ObtenerPorTipoAsync(string tipo);
    }
}
