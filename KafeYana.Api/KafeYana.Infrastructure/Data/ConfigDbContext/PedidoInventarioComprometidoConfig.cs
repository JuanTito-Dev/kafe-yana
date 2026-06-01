using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext;

public class PedidoInventarioComprometidoConfig : IEntityTypeConfiguration<PedidoInventarioComprometido>
{
    public void Configure(EntityTypeBuilder<PedidoInventarioComprometido> builder)
    {
        builder.ToTable("PedidoInventarioComprometido");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Referencia)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FechaCreacion)
            .IsRequired();

        builder.HasOne(x => x.DetalleRonda)
            .WithOne(d => d.CompromisoInventario)
            .HasForeignKey<PedidoInventarioComprometido>(x => x.Id_Detalle_Ronda)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Lineas)
            .WithOne(l => l.Comprometido)
            .HasForeignKey(l => l.Id_Comprometido)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
