using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class Detalle_RondaConfig : IEntityTypeConfiguration<Detalle_ronda>
    {
        public void Configure(EntityTypeBuilder<Detalle_ronda> builder)
        {
            builder.ToTable("Detalle_Ronda");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id_Ronda).IsRequired();

            builder.Property(x => x.Id_Producto).IsRequired();

            builder.Property(x => x.Nombre_Producto)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Cantidad)
                .IsRequired();

            builder.Property(x => x.Precio)
                .IsRequired()
                .HasColumnType("decimal(10,2)")
                .HasDefaultValue(0.00M);

            // Índice único: combinación de Id_Ronda e Id_Producto no se repite
            builder.HasIndex(x => new { x.Id_Ronda, x.Id_Producto })
                .IsUnique()
                .HasDatabaseName("ix_detalle_ronda_ronda_producto_unique");

            // Relación con Ronda
            builder.HasOne(x => x.ronda)
                .WithMany(x => x.Detalle)
                .HasForeignKey(x => x.Id_Ronda)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación con Producto
            builder.HasOne(x => x.producto)
                .WithMany()
                .HasForeignKey(x => x.Id_Producto)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
