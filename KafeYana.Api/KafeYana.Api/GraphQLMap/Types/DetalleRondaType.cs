using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class DetalleRondaType : ObjectType<Detalle_ronda>
    {
        protected override void Configure(IObjectTypeDescriptor<Detalle_ronda> descriptor)
        {
            descriptor.Field(x => x.Id_Ronda).Type<IntType>();
            descriptor.Field(x => x.Id_Producto).Type<IntType>();
            descriptor.Field(x => x.Nombre_Producto).Type<StringType>();
            descriptor.Field(x => x.Cantidad).Type<IntType>();
            descriptor.Field(x => x.Precio).Type<DecimalType>();
            descriptor.Field(x => x.ronda).Type<RondaType>();
            descriptor.Field(x => x.producto).Ignore();
            descriptor.Field(x => x.Opciones).Type<ListType<DetalleRondaOpcionType>>();
        }
    }
}
