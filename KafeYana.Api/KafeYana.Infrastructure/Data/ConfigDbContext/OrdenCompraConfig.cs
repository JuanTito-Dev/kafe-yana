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
    public class OrdenCompraConfig : IEntityTypeConfiguration<OrdenCompra>
    {
        public void Configure(EntityTypeBuilder<OrdenCompra> builder)
        {
            builder.ToTable("OrdenCompra");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo).IsRequired();

            builder.Property(x => x.Nombre_Proveedor)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Fecha)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.Recibido)
                .HasDefaultValue(false);

            builder.Property(x => x.Total)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(x => x.Nota)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.HasOne(x => x.Proveedor)
                .WithMany(x => x.Ordenes)
                .HasForeignKey(x => x.Id_Proveedor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_ordencompra_proveedor");
        }
    }
}
