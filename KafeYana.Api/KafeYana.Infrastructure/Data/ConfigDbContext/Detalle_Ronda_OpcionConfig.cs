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
    public class Detalle_Ronda_OpcionConfig : IEntityTypeConfiguration<Detalle_Ronda_Opcion>
    {
        public void Configure(EntityTypeBuilder<Detalle_Ronda_Opcion> builder)
        {
            builder.ToTable("Detalle_Ronda_Opcion");

            builder.HasKey(x => new { x.Id_Detalle_Ronda, x.Id_Opcion });

            builder.HasOne(x => x.Detalle_Ronda)
                .WithMany(x => x.Opciones)
                .HasForeignKey(x => x.Id_Detalle_Ronda)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_detallerondaopcion_detalleronda");

            builder.HasOne(x => x.Opcion)
                .WithMany()
                .HasForeignKey(x => x.Id_Opcion)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_detallerondaopcion_opcion");
        }
    }
}