using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class ParaLlevarType : ObjectType<ParaLlevar>
    {
        protected override void Configure(IObjectTypeDescriptor<ParaLlevar> descriptor)
        {
            descriptor.Field(x => x.Pedido).Type<PedidoType>();
        }
    }
}
