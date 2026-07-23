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

            // Correlativo online y correlativo CAFC (motivos 5/6/7) son rangos
            // independientes autorizados por el SIN (ver IVentaRepositorio.SiguienteNumeroFacturaCafcAsync).
            // Un solo indice unico global chocaria entre ambos rangos; se parte
            // por modalidad (Cafc NULL = online, Cafc NOT NULL = CAFC).
            builder.HasIndex(x => x.NumeroFactura)
                .IsUnique()
                .HasDatabaseName("IX_Venta_NumeroFactura")
                .HasFilter("\"NumeroFactura\" IS NOT NULL AND \"Cafc\" IS NULL");
            builder.HasIndex(x => x.NumeroFactura)
                .IsUnique()
                .HasDatabaseName("IX_Venta_NumeroFactura_Cafc")
                .HasFilter("\"NumeroFactura\" IS NOT NULL AND \"Cafc\" IS NOT NULL");
            builder.HasIndex(x => x.Facturado);
            builder.HasIndex(x => x.Cuf).IsUnique();
            builder.HasIndex(x => x.EstadoSiat);
            builder.HasIndex(x => x.FechaEmision);

            // Obligatorios
            builder.Property(x => x.RazonSocialEmisor).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Municipio).IsRequired().HasMaxLength(100);

            // Cuf y Cufd pueden medir hasta ~133 / ~88 chars en modo contingencia:
            // ConstruirVentaOfflineAsync usa contingencia.CufdEvento completo (el CUFD
            // Base64 de ~88 chars) como CodigoControl del CUF, y CufGenerator.Generar
            // concatena Base16(cadena54) (~45 chars) + CodigoControl. Subimos de 100 a
            // 500 para que el INSERT no reviente con PostgreSQL 22001. En línea el CUF
            // mide ~45-55 chars, bien dentro del nuevo límite.
            builder.Property(x => x.Cuf).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Cufd).IsRequired().HasMaxLength(500);
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

            // FK a EventoSignificativoSiat (nullable — solo se popula en contingencia).
            // OnDelete=Restrict: no permitir borrar un evento con ventas referenciadas.
            builder.HasOne(x => x.EventoSignificativoSiat)
                .WithMany()
                .HasForeignKey(x => x.EventoSignificativoSiatId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices para la query de reenvío: WHERE EstadoSiat=1 AND EventoSignificativoSiatId=X.
            builder.HasIndex(x => x.EventoSignificativoSiatId)
                .HasDatabaseName("IX_Venta_EventoSignificativoSiatId");
            builder.HasIndex(x => new { x.EstadoSiat, x.EventoSignificativoSiatId })
                .HasDatabaseName("IX_Venta_EstadoSiat_EventoSignificativoSiatId");
        }
    }
}
