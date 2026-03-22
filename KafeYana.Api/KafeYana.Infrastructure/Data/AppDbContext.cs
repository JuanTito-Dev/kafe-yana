using KafeYana.Core.Entities.Entity;
using KafeYana.Infrastructure.Data.ConfigDbContext.Indentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(options)
    {

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(UsuarioConfig).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(RefreshTokenConfig).Assembly);
        }
    }
}
