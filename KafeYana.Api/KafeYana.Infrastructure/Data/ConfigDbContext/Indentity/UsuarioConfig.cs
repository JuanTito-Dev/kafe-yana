using KafeYana.Core.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext.Indentity
{
    internal class UsuarioConfig : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("usuario");

            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Id);

            builder.Property(x => x.Email).IsRequired().HasMaxLength(128);

            builder.HasIndex(x => x.NormalizedEmail).IsUnique();

            builder.Property(x => x.PhoneNumber).HasMaxLength(20);

            builder.HasIndex(x => x.PhoneNumber).IsUnique();

            builder.Property(x => x.Nombre).HasMaxLength(50).IsRequired() ;

            builder.Property(x => x.Apellido).HasMaxLength(50).IsRequired();
        }
    }
}
