using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class HistorialPromocionTemporadaConfig : IEntityTypeConfiguration<HistorialPromocionTemporada>
    {
        public void Configure(EntityTypeBuilder<HistorialPromocionTemporada> builder)
        {
            builder.ToTable("HistorialPromocionTemporada");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoReclamo).IsRequired().HasMaxLength(60);
            builder.Property(x => x.Fecha).IsRequired();

            builder.HasIndex(x => new { x.Id_Cliente, x.Id_PromocionTemporada })
                .IsUnique()
                .HasDatabaseName("IX_HistorialPromocionTemporada_Cliente_Promocion");

            builder.HasIndex(x => x.Id_Cliente)
                .HasDatabaseName("IX_HistorialPromocionTemporada_Cliente");

            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.Id_Cliente)
                .HasConstraintName("fk_historialpromociontemporada_cliente")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.PromocionTemporada)
                .WithMany()
                .HasForeignKey(x => x.Id_PromocionTemporada)
                .HasConstraintName("fk_historialpromociontemporada_promocion")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
