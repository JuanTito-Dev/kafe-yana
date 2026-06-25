using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    /// <summary>
    /// Tipo GraphQL para <see cref="NotaAjusteDetalle"/>. Mínimo: solo evita la navegación
    /// inversa a <c>NotaAjuste</c> para cortar el ciclo. Los campos "hoja"
    /// (CodigoProducto, Cantidad, SubTotal, etc.) los infiere HotChocolate por convención.
    /// </summary>
    public class NotaAjusteDetalleType : ObjectType<NotaAjusteDetalle>
    {
        protected override void Configure(IObjectTypeDescriptor<NotaAjusteDetalle> descriptor)
        {
            descriptor.Field(d => d.NotaAjuste).Ignore();
        }
    }
}
