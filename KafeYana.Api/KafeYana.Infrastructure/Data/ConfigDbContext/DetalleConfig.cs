using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class DetalleConfig : IEntityTypeConfiguration<Detalle>
    {
        public void Configure(EntityTypeBuilder<Detalle> builder)
        {
            builder.ToTable("Detalle");

            builder.HasKey(x => new { x.Id_receta, x.Id_insumo });

            builder.Property(x => x.Cantidad).IsRequired().HasColumnType("decimal(10,2)");

            builder.Property(x => x.Merma).IsRequired().HasColumnType("decimal(10,2)");

            builder.Property(x => x.SubTotal).IsRequired().HasColumnType("decimal(10,2)");

            builder.HasOne(x => x.Receta)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.Id_receta)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Insumo)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.Id_insumo)
                .OnDelete(DeleteBehavior.Cascade);
        
        }
    }
}
