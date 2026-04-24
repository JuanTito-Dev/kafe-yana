using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Api.Schema.Types
{
    public class ImportacionType : ObjectType<Importacion>
    {
        protected override void Configure(IObjectTypeDescriptor<Importacion> importacion)
        {
            base.Configure(importacion);
            importacion.Field(x => x.Proveedor).Type<ProveedorType>();
        }
    }
}
