using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CatTipoEmisionConfig : IEntityTypeConfiguration<CatTipoEmision>
    {
        public void Configure(EntityTypeBuilder<CatTipoEmision> builder)
        {
            builder.ToTable("CatTiposEmision");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo).IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.FechaSincronizacion).IsRequired();

            // Código oficial del SIN, único por tipo de emisión.
            builder.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_CatTiposEmision_Codigo");
        }
    }
}
