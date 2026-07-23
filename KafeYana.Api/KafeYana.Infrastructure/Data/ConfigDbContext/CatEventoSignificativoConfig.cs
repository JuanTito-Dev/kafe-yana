using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CatEventoSignificativoConfig : IEntityTypeConfiguration<CatEventoSignificativo>
    {
        public void Configure(EntityTypeBuilder<CatEventoSignificativo> builder)
        {
            builder.ToTable("CatEventosSignificativos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo)
                .IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.FechaSincronizacion)
                .IsRequired();

            // Código oficial del SIN, único por evento.
            builder.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_CatEventosSignificativos_Codigo");
        }
    }
}
