using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ClienteQuery
    {
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Cliente> Clientes([Service]IClienteRespositorio _clientes)
        {
            return _clientes.GetClientes().AsNoTracking();
        }

        //[Authorize(Roles = new[] { "Admin" })]
        //public async Task<Cliente?> Cliente([Service] IClienteRespositorio _clientes, int Id )
        //{
        //    return await _clientes.GetCliente( Id );
        //}
    }
}
