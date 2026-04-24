using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class RondaConfig : IEntityTypeConfiguration<Ronda>
    {
        public void Configure(EntityTypeBuilder<Ronda> builder)
        {
            builder.ToTable("Ronda");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id_Pedido)
                .IsRequired();

            builder.Property(x => x.Ronda_Descripcion);

            builder.Property(x => x.SubTotal).HasPrecision(18, 2);

            builder.HasOne(p => p.pedido)
                .WithMany(x => x.Rondas)
                .HasForeignKey(p => p.Id_Pedido)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
