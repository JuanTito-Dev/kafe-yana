using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class PromocionPermanenteConfig : IEntityTypeConfiguration<PromocionPermanente>
    {
        public void Configure(EntityTypeBuilder<PromocionPermanente> builder)
        {
            builder.ToTable("PromocionPermanente");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Nombre)
                .IsUnique()
                .HasDatabaseName("IX_PromocionPermanente_Nombre_Unique");

            builder.Property(x => x.Descripcion)
                .HasMaxLength(300)
                .HasDefaultValue(string.Empty);

            builder.Property(x => x.TipoCondicion)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(x => x.ValorCondicion).IsRequired();

            builder.Property(x => x.TipoRecompensa)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(x => x.ValorRecompensa).IsRequired();

            builder.Property(x => x.Activo)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.Id_ProductoCanjeable);

            builder.HasOne(x => x.ProductoCanjeable)
                .WithMany()
                .HasForeignKey(x => x.Id_ProductoCanjeable)
                .HasConstraintName("fk_promocionpermanente_productocanjeable")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Activo)
                .HasDatabaseName("IX_PromocionPermanente_Activo");

            builder.HasIndex(x => x.TipoCondicion)
                .HasDatabaseName("IX_PromocionPermanente_TipoCondicion");
        }
    }
}
