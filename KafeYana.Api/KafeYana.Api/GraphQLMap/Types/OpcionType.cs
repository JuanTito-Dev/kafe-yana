using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class OpcionType : ObjectType<Opcion>
    {
        protected override void Configure(IObjectTypeDescriptor<Opcion> descriptor)
        {
            base.Configure(descriptor);
            descriptor.Field(x => x.Id);
            descriptor.Field(x => x.Nombre);
            descriptor.Field(x => x.AjustePrecio);
            descriptor.Field(x => x.TipoOpcion);
            descriptor.Field(x => x.ValorAnterior);
            descriptor.Field(x => x.Id_variacion);
            descriptor.Field(x => x.Variacion).Type<VariacionType>();
            descriptor.Field(x => x.Ajustes).Type<ListType<AjusteType>>();
        }
    }
}
