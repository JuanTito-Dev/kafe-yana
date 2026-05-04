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
    public class RecetaConfig : IEntityTypeConfiguration<Receta>
    {
        public void Configure(EntityTypeBuilder<Receta> builder)
        {
            builder.ToTable("Receta");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).UseIdentityColumn();

            builder.HasIndex(x => x.Nombre).IsUnique().HasDatabaseName("receta_nombre");

            builder.HasIndex(x => x.Id_Elaborado)
           .IsUnique()
           .HasDatabaseName("ix_receta_id_elaborado");

            builder.Property(x => x.Porciones).IsRequired();
            builder.Property(x => x.Id_Elaborado).IsRequired(false);

            builder.HasOne(x => x.Elaborado)
                   .WithOne(x => x.Receta)
                   .HasForeignKey<Receta>(x => x.Id_Elaborado)
                   .OnDelete(DeleteBehavior.SetNull)
                   .HasConstraintName("fx_elaborado_receta");

        }
    }
}
