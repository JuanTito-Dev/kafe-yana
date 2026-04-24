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
    public class ClienteConfig : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Celular).IsRequired().HasMaxLength(20);
            builder.Property(c => c.Correo).HasConversion(v => v.ToLower(), // Convert to lowercase when saving to the database
                v => v // No conversion when reading from the database
            ).HasMaxLength(100);

            builder.Property(x => x.Correonormalizado).HasConversion(x => x.ToUpper(), x => x);
            builder.Property(x => x.Dni);

            builder.Property(x => x.Direccion).HasMaxLength(200);

            builder.Property(x => x.Estado).HasDefaultValue(true);

            builder.HasIndex(x => x.Nombre).IsUnique();
            builder.HasIndex(x => x.Celular).IsUnique();
            builder.HasIndex(x => x.Correo).IsUnique().HasFilter("\"Correo\" <> ''");
            builder.HasIndex(x => x.Dni).IsUnique().HasFilter("\"Dni\" IS NOT NULL");
            builder.HasIndex(x => x.Correonormalizado).IsUnique().HasFilter("\"Correonormalizado\" <> ''");
        }
    }
}
