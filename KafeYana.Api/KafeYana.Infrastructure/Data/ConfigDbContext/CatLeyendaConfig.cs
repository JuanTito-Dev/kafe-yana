using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CatLeyendaConfig : IEntityTypeConfiguration<CatLeyenda>
    {
        public void Configure(EntityTypeBuilder<CatLeyenda> builder)
        {
            builder.ToTable("CatLeyendas");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoActividad)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.DescripcionLeyenda)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.FechaSincronizacion)
                .IsRequired();

            // PK lógica: la combinación (CodigoActividad, DescripcionLeyenda)
            // debe ser única. Una misma actividad puede tener N leyendas
            // (distintas cláusulas de la Ley 453), pero la misma leyenda no
            // debe repetirse para la misma actividad.
            builder.HasIndex(x => new { x.CodigoActividad, x.DescripcionLeyenda })
                .IsUnique()
                .HasDatabaseName("IX_CatLeyendas_CodigoActividad_DescripcionLeyenda");
        }
    }
}
