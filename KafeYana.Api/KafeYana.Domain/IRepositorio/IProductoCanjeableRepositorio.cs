using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IProductoCanjeableRepositorio : IGenericRepositorio<ProductoCanjeable>
    {
        IQueryable<ProductoCanjeable> GetCanjeables();
    }
}
