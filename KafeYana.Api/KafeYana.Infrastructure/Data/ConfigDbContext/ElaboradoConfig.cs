using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class ElaboradoConfig : IEntityTypeConfiguration<Elaborado>
    {
        public void Configure(EntityTypeBuilder<Elaborado> builder)
        {
            builder.ToTable("Elaborado");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Unidad_medida).IsRequired().HasMaxLength(20);

            builder.Property(x => x.Producible).HasDefaultValue(false);

            // Relacion 1 a 1 con Producto
            builder.HasOne(x => x.Producto)
                .WithOne(p => p.Elaborado)
                .HasForeignKey<Elaborado>(x => x.Id_Producto)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Id_Producto).IsUnique();
        }
    }
}
