using HotChocolate.Authorization;
using KafeYana.Api.GraphQLMap.Types;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ProveedorQuery
    {
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { "Admin" })]
        [GraphQLType(typeof(ListType<ProveedorType>))]
        public IQueryable<Proveedor> Proveedores([Service] IProveedorRepositorio _repository)
        {
            return _repository.Query();
        }
    }
}