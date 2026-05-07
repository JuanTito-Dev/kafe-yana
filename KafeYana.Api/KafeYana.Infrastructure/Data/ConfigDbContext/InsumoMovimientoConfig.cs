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
    public class InsumoMovimientoConfig : IEntityTypeConfiguration<InsumoMovimiento>
    {
        public void Configure(EntityTypeBuilder<InsumoMovimiento> builder)
        {
            builder.ToTable("InsumoMovimiento");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Fecha).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.Tipo).IsRequired();

            builder.Property(x => x.Referencia).IsRequired();

            builder.Property(x => x.Cantidad).IsRequired();

            builder.Property(x => x.Costo_Unitario).HasPrecision(14, 7).IsRequired();

            builder.Property(x => x.Total).HasPrecision(14, 7).IsRequired();

            builder.Property(x => x.Stock_resultante).IsRequired();

            builder.Property(x => x.Id_insumo).IsRequired();

            builder.HasOne(x => x.Insumo)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.Id_insumo)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fx_insumo_movimiento");

        }
    }
}