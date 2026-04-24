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
    internal class AjusteConfig : IEntityTypeConfiguration<Ajuste>
    {
        public void Configure(EntityTypeBuilder<Ajuste> builder)
        {
            builder.ToTable("Ajuste");

            builder.HasKey(x => new { x.Id_Insumo, x.Id_Opcion });

            builder.Property(x => x.Cantidad).IsRequired().HasColumnType("decimal(10,2)");

            builder.Property(x => x.TipoAjuste)
            .IsRequired()
            .HasMaxLength(20);

            // Si se borra Opcion se borran sus AjusteInsumos
            builder.HasOne(x => x.Opcion)
                .WithMany(o => o.Ajustes)
                .HasForeignKey(x => x.Id_Opcion)
                .OnDelete(DeleteBehavior.Cascade);

            // Si se borra Insumo se borran sus AjusteInsumos
            builder.HasOne(x => x.InsumoBase)
                .WithMany(i => i.Ajustes)
                .HasForeignKey(x => x.Id_Insumo)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.InsumoNuevo)
                .WithMany(i => i.AjustesComoNuevo)
                .HasForeignKey(x => x.Id_InsumoNuevo)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices para búsquedas por id
            builder.HasIndex(x => x.Id_Opcion);
            builder.HasIndex(x => x.Id_Insumo);
            builder.HasIndex(x => x.Id_InsumoNuevo);
        }
    }
}
