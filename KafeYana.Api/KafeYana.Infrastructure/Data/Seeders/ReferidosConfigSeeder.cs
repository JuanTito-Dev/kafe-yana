using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Seeders
{
    public static class ReferidosConfigSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.ReferidosConfigs.AnyAsync()) return;

            await db.ReferidosConfigs.AddAsync(new ReferidosConfig
            {
                PuntosReferidor = 0,
                PuntosReferido = 0,
                Activo = false
            });

            await db.SaveChangesAsync();
        }
    }
}
