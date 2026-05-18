using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class HistorialReferidoConfig : IEntityTypeConfiguration<HistorialReferido>
    {
        public void Configure(EntityTypeBuilder<HistorialReferido> builder)
        {
            builder.ToTable("HistorialReferido");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.NombreReferidor).IsRequired().HasMaxLength(150);

            builder.Property(x => x.NombreReferido).IsRequired().HasMaxLength(150);

            builder.Property(x => x.PuntosReferidor).IsRequired();

            builder.Property(x => x.PuntosReferido).IsRequired();

            builder.Property(x => x.Fecha).IsRequired().HasDefaultValueSql("NOW()");

            builder.HasIndex(x => x.Fecha).HasDatabaseName("IX_HistorialReferido_Fecha");
        }
    }
}
