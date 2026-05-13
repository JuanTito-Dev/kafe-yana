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
    public class OrdenItemProductoConfig : IEntityTypeConfiguration<OrdenItemProducto>
    {
        public void Configure(EntityTypeBuilder<OrdenItemProducto> builder)
        {
            builder.ToTable("OrdenItemProducto");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Cantidad)
                .IsRequired();

            builder.Property(x => x.Precio)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(x => x.Subtotal)
                .HasPrecision(10, 2);

            builder.Property(x => x.Id_Producto).IsRequired(false);

            builder.HasOne(x => x.Orden)
                .WithMany(x => x.Productos)
                .HasForeignKey(x => x.Id_Orden)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ordenitemproducto_orden");

            builder.HasOne(x => x.Producto)
                .WithMany(x => x.OrdenesProducto)
                .HasForeignKey(x => x.Id_Producto)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ordenitemproducto_producto");
        }
    }
}
