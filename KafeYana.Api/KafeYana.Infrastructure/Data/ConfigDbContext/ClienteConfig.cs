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
            builder.Property(c => c.Codigo).IsRequired().HasMaxLength(20);
            builder.HasIndex(c => c.Codigo).IsUnique().HasDatabaseName("IX_Cliente_Codigo");
            builder.Property(c => c.Celular).IsRequired().HasMaxLength(20);
            builder.Property(c => c.Correo).HasConversion(v => v.ToLower(), // Convert to lowercase when saving to the database
                v => v // No conversion when reading from the database
            ).HasMaxLength(100);

            builder.Property(x => x.Correonormalizado).HasConversion(x => x.ToUpper(), x => x);
            builder.Property(x => x.Dni);

            builder.Property(x => x.Direccion).HasMaxLength(200);

            builder.Property(x => x.Estado).HasDefaultValue(true);

            builder.Property(x => x.Puntos)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.NumeroCompras)
                .IsRequired()
                .HasDefaultValue(0);
            
            builder.ToTable(t => t.HasCheckConstraint("CK_Cliente_Puntos_NonNegative", "\"Puntos\" >= 0"));

            builder.HasIndex(x => x.Nombre).IsUnique().HasDatabaseName("unique_nombre_cliente");
            builder.HasIndex(x => x.Celular).IsUnique().HasDatabaseName("Unique_celular_cliente");
            builder.HasIndex(x => x.Correo).IsUnique().HasFilter("\"Correo\" <> ''").HasDatabaseName("Unique_correo_cliente");
            builder.HasIndex(x => x.Dni).IsUnique().HasFilter("\"Dni\" IS NOT NULL").HasDatabaseName("Unique_Dni_cliente");
            builder.HasIndex(x => x.Correonormalizado).IsUnique().HasFilter("\"Correonormalizado\" <> ''");
        }
    }
}
