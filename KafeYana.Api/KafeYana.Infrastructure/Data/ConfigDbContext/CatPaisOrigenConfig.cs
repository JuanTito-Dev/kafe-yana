using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CatPaisOrigenConfig : IEntityTypeConfiguration<CatPaisOrigen>
    {
        public void Configure(EntityTypeBuilder<CatPaisOrigen> builder)
        {
            builder.ToTable("CatPaisesOrigen");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo).IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.FechaSincronizacion).IsRequired();

            // Código oficial del SIN, único por país.
            builder.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_CatPaisesOrigen_Codigo");
        }
    }
}