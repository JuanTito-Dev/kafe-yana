using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class PedidoType : ObjectType<Pedido>
    {
        protected override void Configure(IObjectTypeDescriptor<Pedido> descriptor)
        {
            descriptor.Field(p => p.Cliente).Type<ClienteType>();
            descriptor.Field(p => p.Rondas).Type<ListType<RondaType>>();
        }
    }
}
