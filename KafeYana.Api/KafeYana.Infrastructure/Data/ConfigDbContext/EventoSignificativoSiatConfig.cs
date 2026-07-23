using KafeYana.Domain.Entities.Facturacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    /// <summary>
    /// Mapeo EF de <see cref="EventoSignificativoSiat"/>.
    /// Tabla: EventosSignificativosSiat.
    /// </summary>
    public class EventoSignificativoSiatConfig : IEntityTypeConfiguration<EventoSignificativoSiat>
    {
        public void Configure(EntityTypeBuilder<EventoSignificativoSiat> builder)
        {
            builder.ToTable("EventosSignificativosSiat");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoMotivo)
                .IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.FechaHoraInicioEvento)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.FechaHoraFinEvento)
                .HasColumnType("timestamp with time zone");

            builder.Property(x => x.CodigoAmbiente)
                .IsRequired();

            builder.Property(x => x.CodigoPuntoVenta)
                .IsRequired();

            builder.Property(x => x.CodigoSucursal)
                .IsRequired();

            builder.Property(x => x.CodigoSistema)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Nit)
                .IsRequired();

            builder.Property(x => x.Cufd)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.CufdEvento)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Cuis)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.CodigoRecepcionEventoSignificativo)
                .HasMaxLength(50);

            builder.Property(x => x.Transaccion)
                .IsRequired();

            builder.Property(x => x.CodigosRespuestaJson)
                .HasColumnType("text")
                .IsRequired();

            builder.Property(x => x.Origen)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Usuario)
                .HasMaxLength(100);

            builder.Property(x => x.EstadoContingencia)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.FechaRegistro)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()")
                .IsRequired();

            builder.Property(x => x.FechaCierre)
                .HasColumnType("timestamp with time zone");

            // Índice principal: consultas frecuentes del wrapper detector y del
            // endpoint GET /estado filtran por EstadoContingencia='Activo' ordenado
            // por FechaRegistro DESC para resolver la contingencia vigente.
            builder.HasIndex(x => new { x.EstadoContingencia, x.FechaRegistro })
                .HasDatabaseName("IX_EventosSignificativosSiat_Estado_FechaRegistro");

            // Unicidad por codigoRecepcion: el SIN nunca devuelve dos veces el mismo
            // codigo, y nos protege contra doble registro por retry concurrente.
            builder.HasIndex(x => x.CodigoRecepcionEventoSignificativo)
                .IsUnique()
                .HasDatabaseName("IX_EventosSignificativosSiat_CodigoRecepcion")
                .HasFilter("\"CodigoRecepcionEventoSignificativo\" IS NOT NULL");
        }
    }
}