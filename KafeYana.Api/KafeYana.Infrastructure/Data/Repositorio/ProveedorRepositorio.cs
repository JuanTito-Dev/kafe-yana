using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class ProveedorRepositorio : GenericRepositorio<Proveedor>, IProveedorRepositorio
    {
        public ProveedorRepositorio(AppDbContext db) : base(db)
        {
        }

        public async Task<List<Proveedor>> ObtenerTodosAsync()
        {
            return await _db.Proveedores
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Proveedor?> ObtenerPorEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _db.Proveedores
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<bool> ExisteProveedorAsync(int id)
        {
            return await _db.Proveedores
                .AsNoTracking()
                .AnyAsync(x => x.Id == id);
        }
    }
}