using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CatTipoMetodoPagoConfig : IEntityTypeConfiguration<CatTipoMetodoPago>
    {
        public void Configure(EntityTypeBuilder<CatTipoMetodoPago> builder)
        {
            builder.ToTable("CatMetodosPago");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo).IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Activo)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.FechaSincronizacion).IsRequired();

            // Código oficial del SIN, único por método de pago.
            builder.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_CatMetodosPago_Codigo");
        }
    }
}