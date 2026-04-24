using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class Detalle_Ronda_OpcionConfig : IEntityTypeConfiguration<Detalle_Ronda_Opcion>
    {
        public void Configure(EntityTypeBuilder<Detalle_Ronda_Opcion> builder)
        {
            builder.ToTable("Detalle_Ronda_Opcion");

            // Key compuesto: Id_Detalle_Ronda + Id_Opcion
            builder.HasKey(x => new { x.Id_Detalle_Ronda, x.Id_Opcion });

            builder.Property(x => x.Id_Detalle_Ronda).IsRequired();

            builder.Property(x => x.Id_Opcion).IsRequired();

            // Índice para la combinación única
            builder.HasIndex(x => new { x.Id_Detalle_Ronda, x.Id_Opcion })
                .IsUnique()
                .HasDatabaseName("ix_detalle_ronda_opcion_unique");

            // Relación con Opcion
            builder.HasOne(x => x.Opcion)
                .WithOne()
                .HasForeignKey<Detalle_Ronda_Opcion>(x => x.Id_Opcion)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}