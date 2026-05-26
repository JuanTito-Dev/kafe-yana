using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class HistorialHitoCompraConfig : IEntityTypeConfiguration<HistorialHitoCompra>
    {
        public void Configure(EntityTypeBuilder<HistorialHitoCompra> builder)
        {
            builder.ToTable("HistorialHitoCompra");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoReclamo).IsRequired().HasMaxLength(60);
            builder.Property(x => x.NumeroComprasAlReclamar).IsRequired();
            builder.Property(x => x.Fecha).IsRequired();

            builder.HasIndex(x => new { x.Id_Cliente, x.Id_HitoCompra })
                .IsUnique()
                .HasDatabaseName("IX_HistorialHitoCompra_Cliente_Hito");

            builder.HasIndex(x => x.Id_Cliente)
                .HasDatabaseName("IX_HistorialHitoCompra_Cliente");

            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.Id_Cliente)
                .HasConstraintName("fk_historialhitocompra_cliente")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.HitoCompra)
                .WithMany()
                .HasForeignKey(x => x.Id_HitoCompra)
                .HasConstraintName("fk_historialhitocompra_hitocompra")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
