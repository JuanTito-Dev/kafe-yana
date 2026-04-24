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
    public class Stock_AjusteConfig : IEntityTypeConfiguration<Stock_Ajuste>
    {
        public void Configure(EntityTypeBuilder<Stock_Ajuste> builder)
        {
            builder.ToTable("stock_ajuste");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre).IsRequired();

            builder.Property(x => x.Fecha).IsRequired();

            builder.Property(x => x.Tipo).IsRequired();

            builder.Property(x => x.Ajuste).IsRequired();

            builder.Property(x => x.Usuario).IsRequired();

            builder.Property(x => x.Perdida).HasColumnType("decimal(10,2)");
        }
    }
}
