using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext.Indentity
{
    public class RecetaConfig : IEntityTypeConfiguration<Receta>
    {
        public void Configure(EntityTypeBuilder<Receta> builder)
        {
            builder.ToTable("Receta");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nota).HasMaxLength(250).HasDefaultValue(string.Empty);

            builder.HasOne(x => x.Elaborado).WithOne(x => x.Receta)
                .HasForeignKey<Receta>(x => x.Id_Elaborado)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.Id_Elaborado);
        }
    }
}
