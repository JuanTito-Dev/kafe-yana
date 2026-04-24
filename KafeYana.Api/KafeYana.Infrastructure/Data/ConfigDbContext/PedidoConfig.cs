using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class PedidoConfig : IEntityTypeConfiguration<Pedido>
    {
        public void Configure(EntityTypeBuilder<Pedido> builder)
        {
            builder.ToTable("Pedido");

            builder.HasIndex(x => x.Id);

            builder.Property(x => x.Id_Cliente).IsRequired(false);

            builder.Property(x => x.Total).HasPrecision(10,2).HasDefaultValue(0.00M);

            builder.HasOne(x => x.Cliente)
                .WithOne(x => x.Pedido)
                .HasForeignKey<Pedido>(x => x.Id_Cliente)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
