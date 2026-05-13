using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class ParaLlevarConfig : IEntityTypeConfiguration<ParaLlevar>
    {
        public void Configure(EntityTypeBuilder<ParaLlevar> builder)
        {
            builder.ToTable("Parallevar");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Disponible).HasDefaultValue(true);

            builder.Property(x => x.Id_Pedido).IsRequired(false);

            builder.HasOne(x => x.Pedido)
                .WithOne(x => x.ParaLlevar)
                .HasForeignKey<ParaLlevar>(x => x.Id_Pedido)
                .OnDelete(DeleteBehavior.SetNull).HasConstraintName("fx_pedido_parallevar");
        }
    }
}
