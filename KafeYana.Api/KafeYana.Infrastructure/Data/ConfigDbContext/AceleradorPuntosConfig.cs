using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class AceleradorPuntosConfig : IEntityTypeConfiguration<AceleradorPuntos>
    {
        public void Configure(EntityTypeBuilder<AceleradorPuntos> builder)
        {
            builder.ToTable("AceleradorPuntos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Tipo)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(x => x.Tipo)
                .IsUnique()
                .HasDatabaseName("IX_AceleradorPuntos_Tipo_Unique");

            builder.Property(x => x.TipoAplicacion)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Cantidad)
                .IsRequired()
                .HasPrecision(10, 2);

            builder.Property(x => x.UmbralMonto)
                .HasPrecision(10, 2);

            builder.Property(x => x.Activo)
                .IsRequired()
                .HasDefaultValue(false);
        }
    }
}
