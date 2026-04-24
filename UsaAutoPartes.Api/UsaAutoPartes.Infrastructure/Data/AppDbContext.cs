using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities;
using UsaAutoPartes.Domain.Entities.IdentityDb;
using UsaAutoPartes.Infrastructure.Data.ConfigDbContext;

namespace UsaAutoPartes.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<Proveedor> Proveedores { get; set; }

        public DbSet<Importacion> Importaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder Builder)
        {
           base.OnModelCreating(Builder);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigUsuario).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigRefreshToken).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigProducto).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigProveedor).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigImportacion).Assembly);
        }
    }
}
