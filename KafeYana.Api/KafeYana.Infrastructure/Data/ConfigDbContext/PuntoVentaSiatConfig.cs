using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class PuntoVentaSiatConfig : IEntityTypeConfiguration<PuntoVentaSiat>
    {
        public void Configure(EntityTypeBuilder<PuntoVentaSiat> builder)
        {
            builder.ToTable("PuntosVentaSiat");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoSucursal)
                .IsRequired();

            builder.Property(x => x.CodigoPuntoVenta)
                .IsRequired();

            builder.Property(x => x.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Activo)
                .IsRequired();

            builder.Property(x => x.UltimaSyncActividades);

            builder.Property(x => x.UltimaSyncMotivoAnulacion);

            builder.Property(x => x.UltimaSyncActividadesDocumentoSector);

            builder.Property(x => x.UltimaSyncLeyendas);

            builder.Property(x => x.UltimaSyncCodigosSiat);

            builder.Property(x => x.UltimaSyncEventosSignificativos);

            builder.Property(x => x.UltimaSyncPaisOrigen);

            builder.Property(x => x.UltimaSyncTipoDocumentoIdentidad);

            builder.Property(x => x.Cafc)
                .HasMaxLength(50);

            // No puede haber dos PuntosVentaSiat con la misma combinación
            // (sucursal, puntoVenta) — es la PK funcional ante el SIN.
            builder.HasIndex(x => new { x.CodigoSucursal, x.CodigoPuntoVenta })
                .IsUnique()
                .HasDatabaseName("IX_PuntosVentaSiat_Sucursal_PuntoVenta");
        }
    }
}