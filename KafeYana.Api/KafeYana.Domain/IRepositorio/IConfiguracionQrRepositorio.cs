using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IConfiguracionQrRepositorio : IGenericRepositorio<ConfiguracionQr>
    {
        Task<ConfiguracionQr?> ObtenerUnicaAsync();
        Task<bool> ExisteAlgunaAsync();
    }
}
