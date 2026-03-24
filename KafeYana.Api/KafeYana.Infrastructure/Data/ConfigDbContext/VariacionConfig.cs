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
    internal class VariacionConfig : IEntityTypeConfiguration<Variacion>
    {
        public void Configure(EntityTypeBuilder<Variacion> builder)
        {
            builder.ToTable("Variacion");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(50);

            builder.Property(x => x.Requirido).IsRequired().HasDefaultValue(false);

            builder.HasOne(x => x.Elaborado)
                .WithMany(e => e.Variaciones)
                .HasForeignKey(x => x.Id_Elaborado)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Nombre);

            builder.HasIndex(x => x.Id_Elaborado);

            builder.HasIndex(x => x.Requirido);
        }
    }
}
