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
    public class CajaMovimientoConfig : IEntityTypeConfiguration<CajaMovimiento>
    {
        public void Configure(EntityTypeBuilder<CajaMovimiento> builder)
        {
            builder.ToTable("CajaMovimientos");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Fecha)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.Tipo)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Categoria)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.Referencia)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.Monto)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.HasOne(x => x.Caja)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.Id_Caja)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_cajamovimiento_caja");
        }
    }
}
