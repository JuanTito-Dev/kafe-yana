using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class HitoCompraConfig : IEntityTypeConfiguration<HitoCompra>
    {
        public void Configure(EntityTypeBuilder<HitoCompra> builder)
        {
            builder.ToTable("HitoCompra");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.NumeroCompras).IsRequired();

            builder.HasIndex(x => x.NumeroCompras)
                .IsUnique()
                .HasDatabaseName("IX_HitoCompra_NumeroCompras_Unique");

            builder.Property(x => x.Descripcion)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Icono)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Activo)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne(x => x.ProductoCanjeable)
                .WithMany()
                .HasForeignKey(x => x.Id_ProductoCanjeable)
                .HasConstraintName("fk_hitocompra_productocanjeable")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Activo).HasDatabaseName("IX_HitoCompra_Activo");
        }
    }
}
