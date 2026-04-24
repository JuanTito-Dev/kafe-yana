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
    public class ConfigProveedor : IEntityTypeConfiguration<Proveedor>
    {
        public void Configure(EntityTypeBuilder<Proveedor> builder)
        {
            builder.ToTable("Proveedor");

            builder.HasIndex(x => x.Id);

            builder.HasIndex(x => x.Nombre).IsUnique();

            builder.Property(x => x.Nombre).IsRequired();

            builder.Property(x => x.Pais).IsRequired();

            builder.Property(x => x.Moneda).IsRequired();

            builder.Property(x => x.Terminos).IsRequired();

            builder.Property(x => x.Nombre_Contacto).IsRequired();

            builder.Property(x => x.Email).IsRequired();

            builder.Property(x => x.Estado).HasDefaultValue(true);

            builder.HasIndex(x => x.Estado);

            builder.Property(x => x.Total).HasPrecision(10, 2);
        }
    }
}
