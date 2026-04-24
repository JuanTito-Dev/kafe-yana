using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class AjusteType : ObjectType<Ajuste>
    {
        protected override void Configure(IObjectTypeDescriptor<Ajuste> descriptor)
        {
            descriptor.Field(x => x.InsumoBase).Type<InsumoType>();
            descriptor.Field(x => x.InsumoNuevo).Type<InsumoType>();
        }
    }
}
