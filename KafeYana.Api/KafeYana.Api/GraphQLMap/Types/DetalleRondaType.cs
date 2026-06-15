using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class DetalleRondaType : ObjectType<Detalle_ronda>
    {
        protected override void Configure(IObjectTypeDescriptor<Detalle_ronda> descriptor)
        {
            descriptor.Field(x => x.Id_Ronda).Type<IntType>();
            descriptor.Field(x => x.Id_Producto).Type<NonNullType<IntType>>();
            descriptor.Field(x => x.Nombre_Producto).Type<StringType>();
            descriptor.Field(x => x.Cantidad).Type<IntType>();
            descriptor.Field(x => x.Precio).Type<DecimalType>();
            descriptor.Field(x => x.Nota).Type<StringType>();
            descriptor.Field(x => x.Ubicacion).Type<StringType>();
            descriptor.Field(x => x.Codigo).Type<StringType>();
            descriptor.Field(x => x.CodigoSin).Type<StringType>();
            descriptor.Field(x => x.CodigoUnidadMedida).Type<IntType>();
            descriptor.Field(x => x.Producto).Type<ProductoType>();
            descriptor.Field(x => x.ItemsCombo).Type<ListType<DetalleRondaComboItemType>>();
            descriptor.Field(x => x.ronda).Type<RondaType>().Ignore();
            descriptor.Field(x => x.Opciones).Type<ListType<Detalle_Ronda_OpcionType>>();
        }
    }
}
