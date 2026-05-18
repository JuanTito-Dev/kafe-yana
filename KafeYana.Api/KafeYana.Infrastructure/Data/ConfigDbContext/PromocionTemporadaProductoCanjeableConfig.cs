using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class PromocionTemporadaProductoCanjeableConfig : IEntityTypeConfiguration<PromocionTemporadaProductoCanjeable>
    {
        public void Configure(EntityTypeBuilder<PromocionTemporadaProductoCanjeable> builder)
        {
            builder.ToTable("PromocionTemporada_ProductoCanjeable");

            builder.HasKey(x => new { x.Id_PromocionTemporada, x.Id_ProductoCanjeable })
                .HasName("pk_promociontemporada_productocanjeable");

            builder.HasOne(x => x.PromocionTemporada)
                .WithMany(x => x.ProductosCanjeables)
                .HasForeignKey(x => x.Id_PromocionTemporada)
                .HasConstraintName("fk_promtemp_promociontemporada")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ProductoCanjeable)
                .WithMany()
                .HasForeignKey(x => x.Id_ProductoCanjeable)
                .HasConstraintName("fk_promtemp_productocanjeable")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
