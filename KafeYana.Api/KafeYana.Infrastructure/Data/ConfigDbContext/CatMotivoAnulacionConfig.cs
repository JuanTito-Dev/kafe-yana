using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CatMotivoAnulacionConfig : IEntityTypeConfiguration<CatMotivoAnulacion>
    {
        public void Configure(EntityTypeBuilder<CatMotivoAnulacion> builder)
        {
            builder.ToTable("CatMotivosAnulacion");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo)
                .IsRequired();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.FechaSincronizacion)
                .IsRequired();

            // Un código de motivo no debe repetirse.
            builder.HasIndex(x => x.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_CatMotivosAnulacion_Codigo");
        }
    }
}
