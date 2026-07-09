using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class Detalle_RondaConfig : IEntityTypeConfiguration<Detalle_ronda>
    {
        public void Configure(EntityTypeBuilder<Detalle_ronda> builder)
        {
            builder.ToTable("Detalle_Ronda");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id_Ronda).IsRequired();

            builder.Property(x => x.Nombre_Producto)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Cantidad)
                .IsRequired();

            builder.Property(x => x.Precio)
                .IsRequired()
                .HasColumnType("decimal(10,2)")
                .HasDefaultValue(0.00M);

            builder.Property(x => x.Nota)
                .IsRequired()
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            builder.Property(x => x.Ubicacion)
                .IsRequired()
                .HasMaxLength(200)
                .HasDefaultValue(string.Empty);

            builder.Property(x => x.Id_Producto)
                .IsRequired();

            builder.Property(x => x.Codigo)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue(string.Empty);

            builder.Property(x => x.CodigoSin)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(string.Empty);

            builder.Property(x => x.CodigoUnidadMedida)
                .IsRequired()
                .HasDefaultValue(57);

            builder.Property(x => x.CantidadDescontada)
                .IsRequired()
                .HasDefaultValue(0);

            // Relación con Ronda
            builder.HasOne(x => x.ronda)
                .WithMany(x => x.Detalle)
                .HasForeignKey(x => x.Id_Ronda)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Producto)
                .WithMany(p => p.Detalle_Rondas)
                .HasForeignKey(x => x.Id_Producto)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_detalle_ronda_producto");
        }
    }
}
