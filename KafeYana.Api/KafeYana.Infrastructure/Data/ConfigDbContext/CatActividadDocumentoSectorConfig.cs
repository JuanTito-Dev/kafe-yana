using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    /// <summary>
    /// EF Core configuration para <see cref="CatActividadDocumentoSector"/>.
    /// Tabla <c>CatActividadesDocumentosSector</c> con índice único compuesto
    /// en <c>(CodigoActividad, CodigoDocumentoSector)</c> — la PK lógica que
    /// el SIAT garantiza.
    /// </summary>
    public class CatActividadDocumentoSectorConfig
        : IEntityTypeConfiguration<CatActividadDocumentoSector>
    {
        public void Configure(EntityTypeBuilder<CatActividadDocumentoSector> builder)
        {
            builder.ToTable("CatActividadesDocumentosSector");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoActividad)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.CodigoDocumentoSector)
                .IsRequired();

            builder.Property(x => x.TipoDocumentoSector)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.FechaSincronizacion)
                .IsRequired();

            builder.HasIndex(x => new { x.CodigoActividad, x.CodigoDocumentoSector })
                .IsUnique()
                .HasDatabaseName("IX_CatActividadesDocumentosSector_CodigoActividad_CodigoDocumentoSector");
        }
    }
}