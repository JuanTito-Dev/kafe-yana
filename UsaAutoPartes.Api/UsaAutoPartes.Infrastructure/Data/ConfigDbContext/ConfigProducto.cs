using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.ConfigDbContext
{
    public class ConfigProducto : IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> builder)
        {
            builder.ToTable("Producto");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Codigo).IsUnique().HasDatabaseName("IX_Producto_Codigo");

            builder.HasIndex(x => x.CodigoAux).IsUnique().HasFilter("\"CodigoAux\" <> ''") ;

            builder.HasIndex(x => x.CodigoAux2).IsUnique().HasFilter("\"CodigoAux2\" <> ''"); ;

            builder.Property(x => x.Codigo).IsRequired();

            builder.HasIndex(x => x.Nombre).IsUnique();

            builder.Property(x => x.Costo).HasPrecision(10, 2);

            builder.Property(x => x.Precio).HasPrecision(10, 2);

            builder.Property(x => x.ConversionABs).HasPrecision(10, 2);
        }
    }
}
