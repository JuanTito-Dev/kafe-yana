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
    public class ConfigImportacion : IEntityTypeConfiguration<Importacion>
    {
        public void Configure(EntityTypeBuilder<Importacion> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Codigo).IsUnique();

            builder.HasIndex(x => x.Estado);

            builder.Property(x => x.Codigo).IsRequired();

            builder.Property(x => x.Id_Proveedor).IsRequired();

            builder.Property(x => x.Total).HasPrecision(10, 2);

            builder.HasOne(x => x.Proveedor)
            .WithMany(p => p.Importaciones)
            .HasForeignKey(x => x.Id_Proveedor)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
