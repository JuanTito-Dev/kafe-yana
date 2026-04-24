using KafeYana.Domain.Entities;
using KafeYana.Application.IRepositorio;

namespace KafeYana.Application.IRepositorio
{
    public interface IProveedorRepositorio : IGenericRepositorio<Proveedor>
    {
        Task<List<Proveedor>> ObtenerTodosAsync();
        Task<Proveedor?> ObtenerPorEmailAsync(string email);
        Task<bool> ExisteProveedorAsync(int id);

        
    }
}