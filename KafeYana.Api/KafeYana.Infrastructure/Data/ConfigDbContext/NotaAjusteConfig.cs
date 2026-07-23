using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class NotaAjusteConfig : IEntityTypeConfiguration<NotaAjuste>
    {
        public void Configure(EntityTypeBuilder<NotaAjuste> builder)
        {
            builder.ToTable("NotaAjuste");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Cuf).IsUnique();
            builder.HasIndex(x => x.IdVenta);
            builder.HasIndex(x => x.EstadoSiat);
            builder.HasIndex(x => x.FechaEmision);
            builder.HasIndex(x => x.EventoSignificativoSiatId)
                .HasDatabaseName("IX_NotaAjuste_EventoSignificativoSiatId");

            builder.HasIndex(x => x.NumeroNotaCreditoDebito)
                .IsUnique()
                .HasFilter("\"NumeroNotaCreditoDebito\" IS NOT NULL");

            // Obligatorios
            builder.Property(x => x.RazonSocialEmisor).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Municipio).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Cuf).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Cufd).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Direccion).IsRequired().HasMaxLength(500);
            builder.Property(x => x.FechaEmision).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(x => x.NumeroDocumento).IsRequired().HasMaxLength(50);
            builder.Property(x => x.CodigoCliente).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Leyenda).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Usuario).IsRequired().HasMaxLength(100);
            builder.Property(x => x.NumeroAutorizacionCuf).IsRequired().HasMaxLength(100);

            // Decimal precisión
            builder.Property(x => x.MontoTotalOriginal).HasPrecision(18, 2).IsRequired();
            // DescuentoAdicional: solo aplica al XSD de sector 47 (NCDDE). Nullable
            // para no romper notas de sector 24 ya emitidas.
            builder.Property(x => x.DescuentoAdicional).HasPrecision(18, 2);
            builder.Property(x => x.MontoTotalDevuelto).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.MontoDescuentoCreditoDebito).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.MontoEfectivoCreditoDebito).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.FechaEmisionFactura).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.CodigoDocumentoSector).IsRequired().HasDefaultValue(24);
            builder.Property(x => x.CodigoPuntoVenta).HasDefaultValue(0);
            builder.Property(x => x.CodigoMotivoAjuste).IsRequired();

            // Opcionales
            builder.Property(x => x.Telefono).HasMaxLength(50);
            builder.Property(x => x.NombreRazonSocial).HasMaxLength(200);
            builder.Property(x => x.Complemento).HasMaxLength(10);

            // Proceso recepción
            builder.Property(x => x.CodigoRecepcion).HasMaxLength(100);
            builder.Property(x => x.CodigoHash).HasMaxLength(128);
            builder.Property(x => x.ErrorMensaje).HasMaxLength(1000);
            builder.Property(x => x.XmlBase64).HasColumnType("text");
            builder.Property(x => x.EstadoSiat).HasConversion<int?>();
            builder.Property(x => x.RevertidaAnulacion).IsRequired().HasDefaultValue(false);

            // FK a Venta — Restrict: si la Venta no se puede borrar (protege historial fiscal).
            // IMPORTANTE: declarar la nav inversa `v.NotasAjuste` para que EF NO infiera por
            // convención una segunda relación con FK "VentaId" (que no existe en la BD — la
            // columna real es "IdVenta"). Sin este `.WithMany(v => v.NotasAjuste)`, EF genera
            // SQL con `n."VentaId"` y Postgres devuelve 42703 "column does not exist".
            builder.HasOne(x => x.Venta)
                   .WithMany(v => v.NotasAjuste)
                   .HasForeignKey(x => x.IdVenta)
                   .HasConstraintName("FK_NotaAjuste_Venta")
                   .OnDelete(DeleteBehavior.Restrict);

            // FK a EventoSignificativoSiat — SetNull: si se borra el evento, las
            // notas históricas NO deben borrarse (valen como comprobante fiscal).
            // Sólo se desvinculan; el operador puede reasignar/reprocesar manualmente
            // si fuera necesario. Ver [[kafeyana-contingencia-siat]].
            builder.HasOne(x => x.EventoSignificativoSiat)
                   .WithMany()
                   .HasForeignKey(x => x.EventoSignificativoSiatId)
                   .HasConstraintName("FK_NotaAjuste_EventoSignificativoSiat_EventoSignificativoSiatId")
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
