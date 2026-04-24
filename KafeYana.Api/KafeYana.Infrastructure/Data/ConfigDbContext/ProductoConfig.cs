using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class ProductoConfig : IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> builder)
        {
            builder.ToTable("Producto");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(100);

            builder.HasIndex(x => x.Nombre)
                .IsUnique()
                .HasDatabaseName("id_nombre_producto");

            builder.Property(p => p.Descripcion)
            .HasMaxLength(255)
            .HasDefaultValue(string.Empty);

            builder.Property(p => p.Precio)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

            builder.Property(x => x.Tipo).IsRequired().HasMaxLength(20);

            builder.HasOne(p => p.Categoria).WithMany(x => x.Productos)
                .HasForeignKey(p => p.Categoria_Id).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Tipo);
            builder.HasIndex(x => x.Categoria_Id);
        }
    }
}
