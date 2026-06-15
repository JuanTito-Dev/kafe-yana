using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class Detalle_PagoConfig : IEntityTypeConfiguration<Detalle_Pago>
    {
        public void Configure(EntityTypeBuilder<Detalle_Pago> builder)
        {
            builder.ToTable("Detalle_Pago");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Id_venta);

            // Obligatorios
            builder.Property(x => x.ActividadEconomica).IsRequired().HasMaxLength(200);
            builder.Property(x => x.CodigoProducto).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Descripcion).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Cantidad).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.PrecioUnitario).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.SubTotal).HasPrecision(18, 2).IsRequired();

            // Opcionales
            builder.Property(x => x.MontoDescuento).HasPrecision(18, 2);
            builder.Property(x => x.NumeroSerie).HasMaxLength(100);
            builder.Property(x => x.NumeroImei).HasMaxLength(50);

            builder.HasOne(x => x.Venta)
                   .WithMany(x => x.Detalles)
                   .HasForeignKey(x => x.Id_venta)
                   .HasConstraintName("FK_DetallePago_Venta")
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
