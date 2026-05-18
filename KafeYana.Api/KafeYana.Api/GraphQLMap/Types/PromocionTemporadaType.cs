using KafeYana.Domain.Entities;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class PromocionTemporadaType : ObjectType<PromocionTemporada>
    {
        protected override void Configure(IObjectTypeDescriptor<PromocionTemporada> descriptor)
        {
            descriptor.Description("Promoción vigente solo entre FechaInicio y FechaFin.");
        }
    }
}
