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
    internal class InsumoConfig : IEntityTypeConfiguration<Insumo>
    {
        public void Configure(EntityTypeBuilder<Insumo> builder)
        {
            builder.ToTable("Insumo");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(100);

            builder.Property(x => x.Categoria)
            .IsRequired()
            .HasMaxLength(100);

            builder.Property(x => x.Unidad_min_uso)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Unidad_compra)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Factor_conversion)
            .IsRequired()
            .HasColumnType("decimal(10,4)");

            builder.Property(x => x.Costo)
                .IsRequired().HasColumnType("decimal(10,4)");

            builder.Property(x => x.Stock_actual)
            .IsRequired();

            builder.Property(x => x.Stock_min)
                .IsRequired();

            builder.HasIndex(x => x.Id).IsUnique();

            builder.HasIndex(x => x.Nombre).IsUnique();

            builder.HasIndex(x => x.Categoria);
        }
    }
}
