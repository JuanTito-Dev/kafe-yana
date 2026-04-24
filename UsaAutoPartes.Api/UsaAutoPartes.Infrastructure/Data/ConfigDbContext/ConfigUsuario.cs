using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities.IdentityDb;

namespace UsaAutoPartes.Infrastructure.Data.ConfigDbContext
{
    public class ConfigUsuario : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuario");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre).IsRequired();

            builder.HasIndex(x => x.Email).IsUnique();

            builder.HasIndex(x => x.Email).IsUnique();


        }
    }
}
