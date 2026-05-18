using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KafeYana.Infrastructure.Data.ConfigDbContext
{
    public class ConfiguracionQrConfig : IEntityTypeConfiguration<ConfiguracionQr>
    {
        public void Configure(EntityTypeBuilder<ConfiguracionQr> builder)
        {
            builder.ToTable("ConfiguracionQr");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Url)
                .HasMaxLength(2048)
                .IsRequired();
        }
    }
}
