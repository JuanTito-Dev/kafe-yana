using KafeYana.Core.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.ConfigDbContext.Indentity
{
    internal class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refreshtoken");

            builder.HasKey(r => r.Id);

            builder.HasIndex(r => r.Id);

            builder.HasIndex(r => r.Token).IsUnique();

            builder.Property(r => r.Token).IsRequired();

            builder.Property(r => r.CreadoEn).IsRequired();

            builder.Property(r => r.ExpiraEn).IsRequired();

            builder.Property(r => r.IsRevoked)
            .HasDefaultValue(false);

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
