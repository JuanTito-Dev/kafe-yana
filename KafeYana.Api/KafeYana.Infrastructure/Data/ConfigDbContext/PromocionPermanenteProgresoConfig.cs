using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class PromocionPermanenteProgresoConfig : IEntityTypeConfiguration<PromocionPermanenteProgreso>
    {
        public void Configure(EntityTypeBuilder<PromocionPermanenteProgreso> builder)
        {
            builder.ToTable("PromocionPermanenteProgreso");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ContadorCompras).IsRequired().HasDefaultValue(0);

            builder.Property(x => x.ReclamoMontoMinimoPendiente).IsRequired().HasDefaultValue(false);

            builder.HasIndex(x => new { x.Id_Cliente, x.Id_PromocionPermanente })
                .IsUnique()
                .HasDatabaseName("IX_PromocionPermanenteProgreso_Cliente_Promo");

            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.Id_Cliente)
                .HasConstraintName("fk_promocionpermanenteprogreso_cliente")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.PromocionPermanente)
                .WithMany()
                .HasForeignKey(x => x.Id_PromocionPermanente)
                .HasConstraintName("fk_promocionpermanenteprogreso_promocion")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
