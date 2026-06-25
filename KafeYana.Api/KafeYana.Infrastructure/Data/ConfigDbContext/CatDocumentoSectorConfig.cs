using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    /// <summary>
    /// EF Core configuration para <see cref="CatDocumentoSector"/>.
    /// Tabla <c>CatDocumentosSector</c> con índice único en <c>CodigoClasificador</c>
    /// (es la PK lógica — el SIAT garantiza códigos únicos por documento sector).
    /// </summary>
    public class CatDocumentoSectorConfig : IEntityTypeConfiguration<CatDocumentoSector>
    {
        public void Configure(EntityTypeBuilder<CatDocumentoSector> builder)
        {
            builder.ToTable("CatDocumentosSector");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CodigoClasificador).IsRequired();
            builder.Property(x => x.Descripcion).IsRequired().HasMaxLength(300);
            builder.Property(x => x.FechaSincronizacion).IsRequired();

            builder.HasIndex(x => x.CodigoClasificador).IsUnique();
        }
    }
}