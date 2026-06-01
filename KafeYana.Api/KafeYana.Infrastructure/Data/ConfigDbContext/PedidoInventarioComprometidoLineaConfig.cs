using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext;

public class PedidoInventarioComprometidoLineaConfig : IEntityTypeConfiguration<PedidoInventarioComprometidoLinea>
{
    public void Configure(EntityTypeBuilder<PedidoInventarioComprometidoLinea> builder)
    {
        builder.ToTable("PedidoInventarioComprometidoLinea");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TipoEntidad)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Referencia)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Costo)
            .HasPrecision(18, 4);
    }
}
