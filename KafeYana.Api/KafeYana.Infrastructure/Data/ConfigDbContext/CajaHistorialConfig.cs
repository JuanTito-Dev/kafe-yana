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
    public class CajaHistorialConfig : IEntityTypeConfiguration<CajaHistorial>
    {
        public void Configure(EntityTypeBuilder<CajaHistorial> builder)
        {
            builder.ToTable("CajaHistorial");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Apertura)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.Cierre)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.SaldoInicial)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(x => x.TotalIngresos)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(x => x.TotalEgresos)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(x => x.TotalVentas)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(x => x.TotalEfectivo)
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00M);

            builder.Property(x => x.TotalTarjeta)
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00M);

            builder.Property(x => x.TotalQr)
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00M);

            builder.Property(x => x.Diferencia)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(x => x.Estado)
                .HasMaxLength(50)
                .IsRequired();
        }
    }
}
