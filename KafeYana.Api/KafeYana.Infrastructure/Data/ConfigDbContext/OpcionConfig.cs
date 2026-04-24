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
    public class OpcionConfig : IEntityTypeConfiguration<Opcion>
    {
        public void Configure(EntityTypeBuilder<Opcion> builder)
        {
            builder.ToTable("Opcion");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AjustePrecio).IsRequired().HasColumnType("decimal(10,2)");

            builder.Property(x => x.TipoOpcion).IsRequired().HasMaxLength(20).HasDefaultValue("normal");

            builder.Property(x => x.ValorAnterior).HasMaxLength(100);

            builder.HasOne(x => x.Variacion)
                .WithMany(x => x.Opciones)
                .HasForeignKey(x => x.Id_variacion)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Id);
            builder.HasIndex(x => x.Id_variacion);


        }
    }
}
