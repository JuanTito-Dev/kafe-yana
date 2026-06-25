using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CatActividadConfig : IEntityTypeConfiguration<CatActividad>
    {
        public void Configure(EntityTypeBuilder<CatActividad> builder)
        {
            builder.ToTable("CatActividades");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoCaeb)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.TipoActividad)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.FechaSincronizacion)
                .IsRequired();

            // Un código CAEB no debe repetirse.
            builder.HasIndex(x => x.CodigoCaeb)
                .IsUnique()
                .HasDatabaseName("IX_CatActividades_CodigoCaeb");
        }
    }
}