using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class ProductoType : ObjectType<Producto>
    {
        protected override void Configure(IObjectTypeDescriptor<Producto> descriptor)
        {
            descriptor.Field(x => x.Id);

            descriptor.Field(x => x.Nombre);

            descriptor.Field(x => x.Descripcion);

            descriptor.Field(x => x.Precio);

            descriptor.Field(x => x.Tipo);

            descriptor.Field(x => x.Categoria_Id).Ignore();

            descriptor.Field(x => x.Categoria).Type<CategoriaForProductosType>();

            descriptor.Field(x => x.Promocion).Ignore();

            descriptor.Field(x => x.Elaborado).Ignore();

            descriptor.Field(x => x.Comprado).Ignore();
        }
    }
}
