using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CajaConfig : IEntityTypeConfiguration<Caja>
    {
        public void Configure(EntityTypeBuilder<Caja> builder)
        {
            builder.ToTable("Caja");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.SaldoInicial)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(x => x.TotalVentas)
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00M);

            builder.Property(x => x.TotalEfectivo)
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00M);

            builder.Property(x => x.TotalTarjeta)
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00M);

            builder.Property(x => x.TotalQr)
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00M);

            builder.Property(x => x.TotalIngresos)
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00M);

            builder.Property(x => x.TotalEgresos)
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00M);

            builder.Ignore(x => x.SaldoEsperado);

            builder.Property(x => x.Abierta)
                .HasDefaultValue(true);

            builder.Property(x => x.FechaApertura)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.FechaCierre)
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(x => x.AbiertaPor)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.CerradaPor)
                .HasMaxLength(100)
                .IsRequired(false);
        }
    }
}
