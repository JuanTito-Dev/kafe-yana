using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class PromocionTemporadaConfig : IEntityTypeConfiguration<PromocionTemporada>
    {
        public void Configure(EntityTypeBuilder<PromocionTemporada> builder)
        {
            builder.ToTable("PromocionTemporada");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(120);

            builder.HasIndex(x => x.Nombre)
                .IsUnique()
                .HasDatabaseName("IX_PromocionTemporada_Nombre_Unique");

            builder.Property(x => x.FechaInicio).IsRequired();

            builder.Property(x => x.FechaFin).IsRequired();

            builder.Property(x => x.Activo)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(x => x.Activo).HasDatabaseName("IX_PromocionTemporada_Activo");
            builder.HasIndex(x => x.FechaInicio).HasDatabaseName("IX_PromocionTemporada_FechaInicio");
            builder.HasIndex(x => x.FechaFin).HasDatabaseName("IX_PromocionTemporada_FechaFin");
        }
    }
}
