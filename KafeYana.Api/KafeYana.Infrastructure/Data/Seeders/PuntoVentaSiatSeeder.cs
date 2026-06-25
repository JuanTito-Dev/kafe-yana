using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Seeders
{
    /// <summary>
    /// Siembra los puntos de venta iniciales que el sistema declara ante el SIAT.
    /// Por ahora son 2: Casa Matriz (0,0) y Sucursal Norte (0,1).
    /// Si ya existen registros en la tabla, no hace nada (idempotente).
    ///
    /// Para agregar más puntos de venta: INSERT manual a la tabla o un endpoint
    /// CRUD futuro (no incluido en esta primera versión).
    /// </summary>
    public static class PuntoVentaSiatSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.PuntosVentaSiat.AnyAsync()) return;

            db.PuntosVentaSiat.AddRange(
                new PuntoVentaSiat
                {
                    CodigoSucursal = 0,
                    CodigoPuntoVenta = 0,
                    Nombre = "Casa Matriz",
                    Activo = true,
                    UltimaSyncActividades = null
                },
                new PuntoVentaSiat
                {
                    CodigoSucursal = 0,
                    CodigoPuntoVenta = 1,
                    Nombre = "Sucursal Norte",
                    Activo = true,
                    UltimaSyncActividades = null
                }
            );

            await db.SaveChangesAsync();
        }
    }
}