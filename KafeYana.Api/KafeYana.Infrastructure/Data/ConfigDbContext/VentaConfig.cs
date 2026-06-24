using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class VentaConfig : IEntityTypeConfiguration<Venta>
    {
        public void Configure(EntityTypeBuilder<Venta> builder)
        {
            builder.ToTable("Venta");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.NumeroFactura)
                .IsUnique()
                .HasFilter("\"NumeroFactura\" IS NOT NULL");
            builder.HasIndex(x => x.Facturado);
            builder.HasIndex(x => x.Cuf).IsUnique();
            builder.HasIndex(x => x.EstadoSiat);
            builder.HasIndex(x => x.FechaEmision);

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

            builder.Property(x => x.MontoTotal).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.MontoTotalSujetoIva).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.TipoCambio).HasPrecision(18, 0).IsRequired();
            builder.Property(x => x.MontoTotalMoneda).HasPrecision(18, 2).IsRequired();

            builder.Property(x => x.CodigoDocumentoSector).IsRequired().HasDefaultValue(1);
            builder.Property(x => x.CodigoPuntoVenta).HasDefaultValue(0);

            // Opcionales
            builder.Property(x => x.Telefono).HasMaxLength(50);
            builder.Property(x => x.NombreRazonSocial).HasMaxLength(200);
            builder.Property(x => x.Complemento).HasMaxLength(10);
            builder.Property(x => x.NumeroTarjeta).HasMaxLength(20);
            builder.Property(x => x.MontoGiftCard).HasPrecision(18, 2);
            builder.Property(x => x.DescuentoAdicional).HasPrecision(18, 2);
            builder.Property(x => x.Cafc).HasMaxLength(50);

            // Proceso recepción
            builder.Property(x => x.CodigoRecepcion).HasMaxLength(100);
            builder.Property(x => x.CodigoHash).HasMaxLength(128);
            builder.Property(x => x.ErrorMensaje).HasMaxLength(1000);
            builder.Property(x => x.XmlBase64).HasColumnType("text");
            builder.Property(x => x.EstadoSiat).HasConversion<int?>();
            builder.Property(x => x.Facturado).IsRequired().HasDefaultValue(false);
            builder.Property(x => x.RevertidaAnulacion).IsRequired().HasDefaultValue(false);
        }
    }
}
