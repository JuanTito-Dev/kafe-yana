using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class NotaAjusteDetalleConfig : IEntityTypeConfiguration<NotaAjusteDetalle>
    {
        public void Configure(EntityTypeBuilder<NotaAjusteDetalle> builder)
        {
            builder.ToTable("NotaAjusteDetalle");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.IdNotaAjuste);
            builder.HasIndex(x => x.IdDetallePagoOriginal);

            // Espejo de Detalle_PagoConfig
            builder.Property(x => x.ActividadEconomica).IsRequired().HasMaxLength(200);
            builder.Property(x => x.CodigoProducto).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Descripcion).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Cantidad).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.PrecioUnitario).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.SubTotal).HasPrecision(18, 2).IsRequired();

            // Opcionales
            builder.Property(x => x.MontoDescuento).HasPrecision(18, 2);

            // NroItem: correlativo 1..N dentro de la nota. Solo se serializa para
            // sector 47 (XSD distinto). Default 0 para mantener compatibilidad con
            // notas existentes de sector 24.
            builder.Property(x => x.NroItem).IsRequired().HasDefaultValue(0);

            // FK a NotaAjuste — Cascade: borrar la nota borra sus líneas
            builder.HasOne(x => x.NotaAjuste)
                   .WithMany(x => x.Detalles)
                   .HasForeignKey(x => x.IdNotaAjuste)
                   .HasConstraintName("FK_NotaAjusteDetalle_NotaAjuste")
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
