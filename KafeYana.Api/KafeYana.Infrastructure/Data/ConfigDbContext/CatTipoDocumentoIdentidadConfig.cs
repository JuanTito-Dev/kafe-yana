using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CatTipoDocumentoIdentidadConfig : IEntityTypeConfiguration<CatTipoDocumentoIdentidad>
    {
        public void Configure(EntityTypeBuilder<CatTipoDocumentoIdentidad> builder)
        {
            builder.ToTable("CatTiposDocumentoIdentidad");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo).IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.FechaSincronizacion).IsRequired();

            // Código oficial del SIN, único por tipo de documento.
            builder.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_CatTiposDocumentoIdentidad_Codigo");
        }
    }
}