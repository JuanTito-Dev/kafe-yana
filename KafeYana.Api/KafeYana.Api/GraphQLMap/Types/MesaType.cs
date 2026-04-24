using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class MesaType : ObjectType<Mesa>
    {
        protected override void Configure(IObjectTypeDescriptor<Mesa> descriptor)
        {
            descriptor.Field(x => x.Id).Type<NonNullType<IdType>>();
            descriptor.Field(x => x.Nombre).Type<NonNullType<StringType>>();
            descriptor.Field(x => x.Disponible).Type<NonNullType<BooleanType>>();
            descriptor.Field(x => x.Id_Pedido).Type<IntType>();
            descriptor.Field(x => x.pedido).Type<PedidoType>();
        }
    }
}
