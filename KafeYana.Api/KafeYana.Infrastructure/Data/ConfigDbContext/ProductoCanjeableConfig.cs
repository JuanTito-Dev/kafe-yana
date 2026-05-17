using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class ProductoCanjeableConfig : IEntityTypeConfiguration<ProductoCanjeable>
    {
        public void Configure(EntityTypeBuilder<ProductoCanjeable> builder)
        {
            builder.ToTable("ProductoCanjeable");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.NombreProducto)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Categoria)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Puntos).IsRequired();

            builder.Property(x => x.Disponible)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(x => x.Activo)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.Id_Producto)
                .HasConstraintName("fk_productocanjeable_producto")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Id_Producto)
                .IsUnique()
                .HasDatabaseName("IX_ProductoCanjeable_Producto_Unique");

            builder.HasIndex(x => x.Disponible)
                .HasDatabaseName("IX_ProductoCanjeable_Disponible");

            builder.HasIndex(x => x.Activo)
                .HasDatabaseName("IX_ProductoCanjeable_Activo");
        }
    }
}
