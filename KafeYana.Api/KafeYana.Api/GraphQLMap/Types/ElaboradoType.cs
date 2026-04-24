using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class ElaboradoType : ObjectType<Elaborado>
    {
        protected override void Configure(IObjectTypeDescriptor<Elaborado> descriptor)
        {

            descriptor.Field(x => x.Id).Ignore();
 

            descriptor.Field(x => x.Producto.Categoria).Ignore();
            descriptor.Field(x => x.Producto.Categoria_Id);


            descriptor.Field(x => x.Receta).Type<RecetaType>();

            descriptor.Field(x => x.Variaciones).Type<ListType<VariacionType>>(); 
        }
    }
}
