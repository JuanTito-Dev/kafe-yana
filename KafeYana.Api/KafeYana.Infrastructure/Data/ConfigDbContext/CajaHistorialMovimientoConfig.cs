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
    public class CajaHistorialMovimientoConfig : IEntityTypeConfiguration<CajaHistorialMovimiento>
    {
        public void Configure(EntityTypeBuilder<CajaHistorialMovimiento> builder)
        {
            builder.ToTable("CajaHistorialMovimientos");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Categoria)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Tipo)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.Monto)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.HasOne(x => x.CajaHistorial)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.Id_CajaHistorial)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_cajahistorialmovimiento_cajahistorial");
        }
    }
}
