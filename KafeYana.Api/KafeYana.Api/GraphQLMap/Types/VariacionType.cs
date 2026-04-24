using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class VariacionType : ObjectType<Variacion>
    {
        protected override void Configure(IObjectTypeDescriptor<Variacion> descriptor)
        {
            base.Configure(descriptor);
            descriptor.Field(v => v.Id);
            descriptor.Field(v => v.Nombre);
            descriptor.Field(v => v.Requerido);
            descriptor.Field(v => v.Id_Elaborado).Ignore();
            descriptor.Field(v => v.Elaborado).Ignore();
            descriptor.Field(v => v.Opciones).Type<ListType<OpcionType>> () ;

        }
    }
}
