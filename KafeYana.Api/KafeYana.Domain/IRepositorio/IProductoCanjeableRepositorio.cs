using KafeYana.Domain.Entities;

namespace KafeYana.Application.IRepositorio
{
    public interface IProductoCanjeableRepositorio : IGenericRepositorio<ProductoCanjeable>
    {
        IQueryable<ProductoCanjeable> GetCanjeables();

        /// <summary>Canjeable con <see cref="ProductoCanjeable.Producto"/> cargado para validaciones.</summary>
        Task<ProductoCanjeable?> ObtenerParaCanjeAsync(int idProductoCanjeable);
    }
}
