using KafeYana.Domain.Entities.Facturacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CodigoSiatConfig : IEntityTypeConfiguration<CodigoSiat>
    {
        public void Configure(EntityTypeBuilder<CodigoSiat> builder)
        {
            builder.ToTable("CodigosSiat");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoProducto)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.DescripcionProducto)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.CodigoActividad)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.DescripcionActividad)
                .HasMaxLength(1000)
                .IsRequired();

            builder.HasIndex(x => x.CodigoProducto)
                .HasDatabaseName("IX_CodigosSiat_CodigoProducto");

            builder.HasIndex(x => x.CodigoActividad)
                .HasDatabaseName("IX_CodigosSiat_CodigoActividad");

            
        }
    }
}
