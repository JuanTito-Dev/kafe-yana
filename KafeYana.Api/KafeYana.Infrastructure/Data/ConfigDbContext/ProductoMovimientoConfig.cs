using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class ProductoMovimientoConfig : IEntityTypeConfiguration<ProductoMovimiento>
    {
        public void Configure(EntityTypeBuilder<ProductoMovimiento> builder)
        {
            builder.ToTable("Movimiento_Producto");

            builder.HasKey(x => x.Id);

            builder.Property(X => X.Fecha).IsRequired();

            builder.Property(x => x.Tipo).HasColumnName("Tipo");

            builder.Property(x => x.Referencia).HasColumnName("Referencia");

            builder.Property(x => x.Cantidad).IsRequired();

            builder.Property(x => x.Costo_Unitario).HasPrecision(10, 2).IsRequired();

            builder.Property(x => x.Total).HasPrecision(10, 2).IsRequired();

            builder.Property(x => x.Stock_resultante).IsRequired();

            builder.Property(x => x.Id_Producto).IsRequired();

            builder.HasOne(x => x.Producto)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.Id_Producto)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fx_producto_movimientos");
        }
    }
}
