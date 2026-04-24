using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class Detalle_ventaConfig : IEntityTypeConfiguration<Detalle_venta>
    {
        public void Configure(EntityTypeBuilder<Detalle_venta> builder)
        {
            builder.ToTable("Detalle_venta");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Id_venta);

            builder.Property(x => x.Precio).HasPrecision(10, 2);

            builder.Property(x => x.Total).HasPrecision(10, 2);

            // Relación
            builder.HasOne(x => x.venta)
                   .WithMany(x => x.Detalles)
                   .HasForeignKey(x => x.Id_venta)
                   .HasConstraintName("FK_DetalleVenta_Venta")
                   .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
