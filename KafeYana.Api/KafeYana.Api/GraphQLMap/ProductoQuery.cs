
using HotChocolate.Authorization;
using KafeYana.Application.Dtos.ProductoDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Infrastructure.Data;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ProductoQuery
    {
        [UsePaging]
        [UseSorting]
        [UseFiltering]
        [Authorize (Roles = new[] { "Admin" })]
        public async Task<IEnumerable<DtoProductosAll>> GetProductos(
        string? Tipo,
        [Service] IProductoRepositorio repo)
        {
            var productos = await repo.GetProductos(Tipo);

            return productos.Select(p => new DtoProductosAll
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Tipo = p.Tipo,

                CategoriaNombre = p.Categoria != null? p.Categoria.Nombre : string.Empty,

                PrecioVenta = p.Precio,
                Costo = p.Comprado != null ? p.Comprado.Costo_compra : 0,

                Stock = p.Comprado != null ? p.Comprado.Stock_actual : 0,

                RecetaName = p.Elaborado != null ? p.Elaborado.Receta.Nota : string.Empty
            });
        }
    }
}
