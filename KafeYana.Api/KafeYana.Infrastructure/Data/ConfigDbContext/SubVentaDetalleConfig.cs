using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class SubVentaDetalleConfig : IEntityTypeConfiguration<SubVentaDetalle>
    {
        public void Configure(EntityTypeBuilder<SubVentaDetalle> builder)
        {
            builder.ToTable("SubVentaDetalle");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id_Producto).IsRequired();
            builder.Property(x => x.Nombre_Producto).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Cantidad).IsRequired();
            builder.Property(x => x.Precio).HasPrecision(10, 2).IsRequired();
            builder.Property(x => x.Codigo).IsRequired().HasMaxLength(10).HasDefaultValue(string.Empty);
            builder.Property(x => x.CodigoSin).IsRequired().HasMaxLength(20).HasDefaultValue(string.Empty);
            builder.Property(x => x.CodigoUnidadMedida).IsRequired().HasDefaultValue(57);

            // OrigenRondaId es solo auditoría: columna plana, sin FK, sin navegación a Detalle_ronda.
            builder.Property(x => x.OrigenRondaId);

            builder.HasOne(x => x.SubVenta)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.Id_SubVenta)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_subventadetalle_subventa");
        }
    }
}
