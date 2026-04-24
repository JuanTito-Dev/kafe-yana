using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class VentaConfig : IEntityTypeConfiguration<Venta>
    {
        public void Configure(EntityTypeBuilder<Venta> builder)
        {
            builder.ToTable("Venta");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Codigo).IsUnique().HasDatabaseName("Codigo-repetido");

            builder.HasIndex(x => x.Cliente);

            builder.HasIndex(x => x.Cajero);

            builder.HasIndex(x => x.Estado);

            builder.Property(x => x.Cliente).IsRequired();

            builder.Property(x => x.Estado).IsRequired();

            builder.Property(x => x.Fecha).HasDefaultValueSql("NOW()");

            builder.Property(x => x.Productos).HasDefaultValue(0);

            builder.Property(x => x.Subtotal).IsRequired().HasPrecision(10, 2);

            builder.Property(x => x.Total).IsRequired().HasPrecision(10, 2);

            builder.Property(x => x.Codigo).HasComputedColumnSql("'VTA-' || CAST(\"Id\" AS VARCHAR)", stored: true);

        }
    }
}
