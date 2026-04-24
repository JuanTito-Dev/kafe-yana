using UsaAutoPartes.Domain.Entities;
using UsaAutoPartes.Domain.Enum.UsuarioEnums;

namespace UsaAutoPartes.Api.Schema.Types
{
    public class ProductoType : ObjectType<Producto>
    {
        protected override void Configure(IObjectTypeDescriptor<Producto> producto)
        {
            base.Configure(producto);
            producto.Field(p => p.Id).Type<NonNullType<IdType>>();
            producto.Field(p => p.Codigo).Type<NonNullType<StringType>>();
            producto.Field(p => p.Nombre).Type<NonNullType<StringType>>();
            producto.Field(p => p.Precio).Authorize(new[] { UsuarioRoles.Admin });
        }
    }
}
