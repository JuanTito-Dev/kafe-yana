using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class ReglaBasePuntosConfig : IEntityTypeConfiguration<ReglaBasePuntos>
    {
        public void Configure(EntityTypeBuilder<ReglaBasePuntos> builder)
        {
            builder.ToTable("ReglaBasePuntos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Cantidad)
                .IsRequired()
                .HasPrecision(10, 2);

            builder.Property(x => x.Activo)
                .IsRequired()
                .HasDefaultValue(true);
        }
    }
}
