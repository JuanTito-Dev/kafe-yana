using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class RondaType : ObjectType<Ronda>
    {
        protected override void Configure(IObjectTypeDescriptor<Ronda> descriptor)
        {
            descriptor.Field(x => x.Id).Type<NonNullType<IdType>>();
            descriptor.Field(x => x.Id_Pedido).Type<IntType>();
            descriptor.Field(x => x.pedido).Type<PedidoType>();
            descriptor.Field(x => x.Detalle).Type<ListType<DetalleRondaType>>();
        }
    }
}
