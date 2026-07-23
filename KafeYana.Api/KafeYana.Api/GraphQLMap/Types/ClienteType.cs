using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class ClienteType : ObjectType<Cliente>
    {
        protected override void Configure(IObjectTypeDescriptor<Cliente> descriptor)
        {
            descriptor.Field(X => X.Correonormalizado).Ignore();
            descriptor.Field(X => X.Pedidos).Ignore();

            // País de origen del documento, sólo presente para clientes extranjeros
            // (CEX / PAS). Expone un objeto { codigo, descripcion } para que la UI
            // pueda mostrarlo directamente ("Pasaporte USA") y autocomplete el
            // dropdown de país en DatosFiscalesForm al seleccionar el cliente.
            descriptor.Field("paisOrigen")
                .Type<ObjectType<PaisOrigenGql>>()
                .Resolve(context =>
                {
                    var cliente = context.Parent<Cliente>();
                    if (cliente.IdPaisOrigen is null || cliente.PaisOrigen is null)
                        return null;
                    return new PaisOrigenGql
                    {
                        Codigo = cliente.PaisOrigen.Codigo,
                        Descripcion = cliente.PaisOrigen.Descripcion
                    };
                });
        }

        /// <summary>
        /// Proyección de <c>CatPaisOrigen</c> para el field <c>paisOrigen</c>
        /// del <c>ClienteType</c>. Expone sólo código + descripción al cliente.
        /// </summary>
        private sealed class PaisOrigenGql
        {
            public int Codigo { get; init; }
            public string Descripcion { get; init; } = string.Empty;
        }
    }
}
