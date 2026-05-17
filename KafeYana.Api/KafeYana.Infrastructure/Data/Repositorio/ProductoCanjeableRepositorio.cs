using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class ProductoCanjeableRepositorio(AppDbContext _db)
        : GenericRepositorio<ProductoCanjeable>(_db), IProductoCanjeableRepositorio
    {
        public IQueryable<ProductoCanjeable> GetCanjeables()
        {
            return _db.ProductosCanjeables.AsNoTracking().AsQueryable();
        }
    }
}
