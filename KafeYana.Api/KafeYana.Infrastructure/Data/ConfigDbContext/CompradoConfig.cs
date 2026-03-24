using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class CompradoConfig : IEntityTypeConfiguration<Comprado>
    {
        public void Configure(EntityTypeBuilder<Comprado> builder)
        {
            builder.ToTable("Comprado");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo_barra).HasMaxLength(50);

            builder.Property(x => x.Unidad_medida).IsRequired().HasMaxLength(20);

            builder.Property(x => x.Marca)
            .HasMaxLength(100);

            builder.Property(x => x.Ubicacion)
                .HasMaxLength(100);

            builder.Property(x => x.Costo_compra).IsRequired().HasColumnType("decimal(10,2)");

            builder.Property(x => x.Stock_actual)
            .IsRequired();

            builder.Property(x => x.Stock_minimo)
                .IsRequired();

            builder.Property(x => x.Disponible)
            .HasDefaultValue(true);

            builder.HasOne(x => x.Producto)
            .WithOne(p => p.Comprado)
            .HasForeignKey<Comprado>(x => x.Id_Producto)
            .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Id_Producto).IsUnique(); // unico por ser 1 a 1
            builder.HasIndex(x => x.Codigo_barra);
            builder.HasIndex(x => x.Disponible);
        }
    }
}
