using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class DetalleRondaComboItemType : ObjectType<Detalle_Ronda_ComboItem>
    {
        protected override void Configure(IObjectTypeDescriptor<Detalle_Ronda_ComboItem> descriptor)
        {
            descriptor.Field(x => x.Id).Type<NonNullType<IdType>>();
            descriptor.Field(x => x.Id_Detalle_Ronda).Ignore();
            descriptor.Field(x => x.Id_Producto).Type<NonNullType<IntType>>();
            descriptor.Field(x => x.Nombre).Type<NonNullType<StringType>>();
            descriptor.Field(x => x.Cantidad).Type<NonNullType<IntType>>();
            descriptor.Field(x => x.Ubicacion).Type<StringType>();
            descriptor.Field(x => x.Detalle_Ronda).Ignore();
            descriptor.Field(x => x.Producto).Ignore();
        }
    }
}
