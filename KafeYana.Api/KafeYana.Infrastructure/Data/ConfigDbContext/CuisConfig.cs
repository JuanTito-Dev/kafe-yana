using KafeYana.Domain.Entities.Facturacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CuisConfig : IEntityTypeConfiguration<Cuis>
    {
        public void Configure(EntityTypeBuilder<Cuis> builder)
        {
            builder.ToTable("Cuis");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.FechaVigencia)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.CodigoSucursal)
                .IsRequired();

            builder.Property(x => x.CodigoPuntoVenta)
                .IsRequired();

            builder.Property(x => x.FechaRegistro)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()")
                .IsRequired();
        }
    }
}
