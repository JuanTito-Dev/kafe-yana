using KafeYana.Core.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class CategoriaForProductosType : ObjectType<Categoria>
    {
        protected override void Configure(IObjectTypeDescriptor<Categoria> descriptor)
        {
            descriptor.Name("CategoriaProducto");
            descriptor.Field(x => x.Productos).Ignore();
        }
    }
}
