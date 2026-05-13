using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class Detalle_Ronda_ComboItemConfig : IEntityTypeConfiguration<Detalle_Ronda_ComboItem>
    {
        public void Configure(EntityTypeBuilder<Detalle_Ronda_ComboItem> builder)
        {
            builder.ToTable("Detalle_Ronda_Combo_Item");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Cantidad)
                .IsRequired();

            builder.Property(x => x.Ubicacion)
                .IsRequired()
                .HasMaxLength(200)
                .HasDefaultValue(string.Empty);

            builder.Property(x => x.Id_Producto)
                .IsRequired();

            builder.HasOne(x => x.Detalle_Ronda)
                .WithMany(x => x.ItemsCombo)
                .HasForeignKey(x => x.Id_Detalle_Ronda)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_detallerondacomboitem_detalleronda");

            builder.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.Id_Producto)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_detallerondacomboitem_producto");
        }
    }
}
