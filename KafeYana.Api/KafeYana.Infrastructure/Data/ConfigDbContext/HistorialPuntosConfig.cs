using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class HistorialPuntosConfig : IEntityTypeConfiguration<HistorialPuntos>
    {
        public void Configure(EntityTypeBuilder<HistorialPuntos> builder)
        {
            builder.ToTable("HistorialPuntos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoVenta)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.PuntosBase).IsRequired();

            builder.Property(x => x.PuntosFinales).IsRequired();

            builder.Property(x => x.Desglose).HasMaxLength(500);

            builder.Property(x => x.Fecha)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.Id_Cliente)
                .HasConstraintName("fk_historialpuntos_cliente")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Id_Cliente).HasDatabaseName("IX_HistorialPuntos_Cliente");

            builder.HasIndex(x => x.CodigoVenta).HasDatabaseName("IX_HistorialPuntos_CodigoVenta");
        }
    }
}
