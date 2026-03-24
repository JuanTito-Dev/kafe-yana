using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    internal class PromocionConfig : IEntityTypeConfiguration<Promocion>
    {
        public void Configure(EntityTypeBuilder<Promocion> builder)
        {
            builder.ToTable("Promocion");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Producto)
            .WithOne(p => p.Promocion)
            .HasForeignKey<Promocion>(x => x.Producto_Id)
            .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Producto_Id).IsUnique();
        }
    }
}
