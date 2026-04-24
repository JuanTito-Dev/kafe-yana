using HotChocolate.Authorization;
using KafeYana.Api.GraphQLMap.Types;
using KafeYana.Application.Dtos.Categoria;
using KafeYana.Application.IRepositorio;
using KafeYana.Core.Entities.Inventario;
using KafeYana.Infrastructure.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class CategoriaQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { "Admin" })]
        [GraphQLType(typeof(ListType<CategoriaType>))]
        public IQueryable<Categoria> Categorias([Service] ICategoriaRepositorio _db)
        {
            return _db.QueryCategorias().AsNoTracking();
        } 

    }
}
