using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class ReferidosConfigConfig : IEntityTypeConfiguration<ReferidosConfig>
    {
        public void Configure(EntityTypeBuilder<ReferidosConfig> builder)
        {
            builder.ToTable("ReferidosConfig");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PuntosReferidor).IsRequired().HasDefaultValue(0);

            builder.Property(x => x.PuntosReferido).IsRequired().HasDefaultValue(0);

            builder.Property(x => x.Activo).IsRequired().HasDefaultValue(false);
        }
    }
}
