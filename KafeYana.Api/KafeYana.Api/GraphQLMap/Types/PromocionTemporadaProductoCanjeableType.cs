using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    /// <summary>Enlace temporada ↔ producto canjeable (evita ciclo GraphQL ignorando la temporada).</summary>
    public class PromocionTemporadaProductoCanjeableType : ObjectType<PromocionTemporadaProductoCanjeable>
    {
        protected override void Configure(IObjectTypeDescriptor<PromocionTemporadaProductoCanjeable> descriptor)
        {
            descriptor.Field(x => x.PromocionTemporada).Ignore();
        }
    }
}
