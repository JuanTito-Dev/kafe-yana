using Microsoft.AspNetCore.Http;

namespace KafeYana.Application.IServicios
{
    public interface IQrImagenService
    {
        Task<string> SubirAsync(IFormFile imagen);
    }
}
