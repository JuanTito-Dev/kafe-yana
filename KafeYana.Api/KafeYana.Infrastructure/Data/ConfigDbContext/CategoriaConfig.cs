using KafeYana.Core.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    internal class CategoriaConfig : IEntityTypeConfiguration<Categoria>
    {
        public void Configure(EntityTypeBuilder<Categoria> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).UseIdentityColumn();

            builder.Property(c => c.Nombre).IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");

            builder.HasIndex(c => c.Nombre).IsUnique().HasDatabaseName("ix_categorias_nombre");

            builder.Property(c => c.Descripcion)
                .HasMaxLength(500)
                .HasColumnType("varchar(500)")
                .HasDefaultValue(string.Empty);

            builder.Property(c => c.Color)
                .IsRequired()
                .HasMaxLength(7)
                .HasColumnType("char(7)");

            builder.Property(c => c.Estado)
                .IsRequired()
                .HasDefaultValue(true);
        }
    }
}
