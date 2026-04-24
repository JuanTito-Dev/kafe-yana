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
    public class MesaConfig : IEntityTypeConfiguration<Mesa>
    {
        public void Configure(EntityTypeBuilder<Mesa> builder)
        {
            builder.ToTable("Mesa");

            builder.HasKey(t => t.Id);

            builder.Property(x => x.Nombre).IsRequired();

            builder.HasIndex(x => x.Nombre).IsUnique();

            builder.Property(x => x.Id_Pedido).IsRequired(false);

            builder.Property(x => x.Disponible).HasDefaultValue(true);

            builder.HasOne(x => x.pedido)
                .WithOne(x => x.Mesa)
                .HasForeignKey<Mesa>(x => x.Id_Pedido)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
