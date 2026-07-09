using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class SubVentaConfig : IEntityTypeConfiguration<SubVenta>
    {
        public void Configure(EntityTypeBuilder<SubVenta> builder)
        {
            builder.ToTable("SubVenta");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Monto).HasPrecision(10, 2).IsRequired();
            builder.Property(x => x.Fecha).IsRequired();
            builder.Property(x => x.CodigoMetodoPago).IsRequired();
            builder.Property(x => x.EsPagoFinal).HasDefaultValue(false);
            builder.Property(x => x.Cajero).IsRequired().HasMaxLength(200).HasDefaultValue(string.Empty);
            builder.Property(x => x.Facturada).IsRequired().HasDefaultValue(false);

            // Restrict (no Cascade): un pedido con sub-ventas registradas nunca debe
            // poder borrarse silenciosamente — eso destruiría historial de cobros
            // (y facturas ya emitidas). El cobro de "100% de una vez" que sí borra
            // el pedido solo debe usarse en pedidos sin sub-ventas previas.
            builder.HasOne(x => x.Pedido)
                .WithMany(x => x.SubVentas)
                .HasForeignKey(x => x.Id_Pedido)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_subventa_pedido");

            builder.HasOne(x => x.Venta)
                .WithMany()
                .HasForeignKey(x => x.Id_Venta)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_subventa_venta");

            builder.HasIndex(x => x.Id_Pedido);
            builder.HasIndex(x => x.Id_Venta);
            builder.HasIndex(x => x.Facturada);
        }
    }
}
