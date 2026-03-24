using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    internal class PromocionDetalleConfig : IEntityTypeConfiguration<PromocionDetalle>
    {
        public void Configure(EntityTypeBuilder<PromocionDetalle> builder)
        {
            builder.ToTable("Detalle_promocion");

            builder.HasKey(x => new { x.Id_Producto, x.Id_Promocion });

            builder.Property(x => x.Cantidad).IsRequired();

            builder.Property(x => x.Opcional).IsRequired();

            // Si se borra Promocion se borran sus detalles
            builder.HasOne(x => x.Promocion)
                .WithMany(p => p.Detalles)
                .HasForeignKey(x => x.Id_Promocion)
                .OnDelete(DeleteBehavior.Cascade);

            // Si se borra Producto se borran sus detalles
            builder.HasOne(x => x.Producto)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.Id_Producto)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
