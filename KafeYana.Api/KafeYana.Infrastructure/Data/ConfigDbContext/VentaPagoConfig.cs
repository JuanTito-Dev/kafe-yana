using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class VentaPagoConfig : IEntityTypeConfiguration<VentaPago>
    {
        public void Configure(EntityTypeBuilder<VentaPago> builder)
        {
            builder.ToTable("VentaPagos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.IdVenta).IsRequired();
            builder.Property(x => x.CodigoMetodoPago).IsRequired();
            builder.Property(x => x.Monto)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // FK a Venta con Cascade: si se borra la venta, se borran sus pagos.
            // Usamos WithMany(v => v.Pagos) para que EF NO genere un FK shadow
            // adicional (la colección de navegación ya está en Venta).
            builder.HasOne<Venta>()
                .WithMany(v => v.Pagos)
                .HasForeignKey(x => x.IdVenta)
                .OnDelete(DeleteBehavior.Cascade);

            // Índice para queries "pagos de esta venta" y para auditoría cruzada.
            builder.HasIndex(x => x.IdVenta)
                .HasDatabaseName("IX_VentaPagos_IdVenta");
        }
    }
}