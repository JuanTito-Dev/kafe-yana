using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    /// <summary>
    /// Tipo GraphQL para <see cref="NotaAjuste"/>. Expone la entidad al esquema HotChocolate
    /// con el nombre de campo <c>notasAjuste</c> dentro de <c>VentaNode</c>.
    /// Ocultamos la navegación inversa <c>Venta</c> porque el JOIN ya carga el padre y
    /// sería un loop; el frontend ya recibe la venta por el resolver padre.
    /// </summary>
    public class NotaAjusteType : ObjectType<NotaAjuste>
    {
        protected override void Configure(IObjectTypeDescriptor<NotaAjuste> descriptor)
        {
            descriptor.Field(n => n.Venta).Ignore();
            descriptor.Field(n => n.Detalles).Type<ListType<NotaAjusteDetalleType>>().Name("detalles");
        }
    }
}
