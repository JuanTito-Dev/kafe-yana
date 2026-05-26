using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class HistorialPromocionPermanenteConfig : IEntityTypeConfiguration<HistorialPromocionPermanente>
    {
        public void Configure(EntityTypeBuilder<HistorialPromocionPermanente> builder)
        {
            builder.ToTable("HistorialPromocionPermanente");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoVenta).IsRequired().HasMaxLength(50);
            builder.Property(x => x.TipoRecompensa).IsRequired().HasMaxLength(30);
            builder.Property(x => x.TipoCondicion).IsRequired().HasMaxLength(30);
            builder.Property(x => x.Fecha).IsRequired();

            builder.HasIndex(x => x.Id_Cliente)
                .HasDatabaseName("IX_HistorialPromocionPermanente_Cliente");

            builder.HasIndex(x => new { x.CodigoVenta, x.TipoRecompensa })
                .IsUnique()
                .HasDatabaseName("IX_HistorialPromocionPermanente_Venta_TipoRecompensa");

            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.Id_Cliente)
                .HasConstraintName("fk_historialpromocionpermanente_cliente")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.PromocionPermanente)
                .WithMany()
                .HasForeignKey(x => x.Id_PromocionPermanente)
                .HasConstraintName("fk_historialpromocionpermanente_promocion")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
