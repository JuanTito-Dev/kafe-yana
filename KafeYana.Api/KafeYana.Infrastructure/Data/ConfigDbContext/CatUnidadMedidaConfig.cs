using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CatUnidadMedidaConfig : IEntityTypeConfiguration<CatUnidadMedida>
    {
        public void Configure(EntityTypeBuilder<CatUnidadMedida> builder)
        {
            builder.ToTable("CatUnidadesMedida");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo).IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Activo)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.FechaSincronizacion).IsRequired();

            // Código oficial del SIN, único por unidad de medida.
            builder.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_CatUnidadesMedida_Codigo");
        }
    }
}