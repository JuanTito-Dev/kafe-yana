using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class CategoriaType : ObjectType<Categoria>
    {
        protected override void Configure(IObjectTypeDescriptor<Categoria> descriptor)
        {
            descriptor.Field(x => x.Id);
            descriptor.Field(x => x.Nombre);
            descriptor.Field(x => x.Descripcion);
            descriptor.Field(x => x.Color);
            descriptor.Field(x => x.Productos).Type<ListType<ProductoType>>();
        }
    }
}
