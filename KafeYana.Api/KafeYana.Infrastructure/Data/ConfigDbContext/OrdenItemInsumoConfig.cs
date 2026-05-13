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
    public class OrdenItemInsumoConfig : IEntityTypeConfiguration<OrdenItemInsumo>
    {
        public void Configure(EntityTypeBuilder<OrdenItemInsumo> builder)
        {
            builder.ToTable("OrdenItemInsumo");
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

            builder.Property(x => x.Id_Insumo).IsRequired(false);

            builder.HasOne(x => x.Orden)
                .WithMany(x => x.Insumos)
                .HasForeignKey(x => x.Id_Orden)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ordeniteminsumo_orden");

            builder.HasOne(x => x.Insumo)
                .WithMany(x => x.OrdenesInsumo)
                .HasForeignKey(x => x.Id_Insumo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ordeniteminsumo_insumo");
        }
    }
}
