using KafeYana.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Seeders
{
    /// <summary>
    /// Siembra los puntos de venta del negocio registrados ante el SIN.
    /// Por defecto deja SOLO el PV 0 activo (la caja principal).
    ///
    /// Reglas de operación:
    ///   - El sistema trabaja con exactamente UN PV activo a la vez.
    ///     Para cambiar de caja: UPDATE manual en BD
    ///       UPDATE "PuntosVentaSiat" SET "Activo" = ("CodigoPuntoVenta" = N);
    ///   - appsettings.json (Siat:CodigoSucursal / Siat:CodigoPuntoVenta)
    ///     es solo el ÚLTIMO fallback si no hay ningún PV activo en BD.
    ///   - Para registrar un nuevo PV ante el SIN, primero hay que darlo de
    ///     alta en el portal del SIAT, después INSERT en esta tabla.
    ///
    /// Idempotente: si ya hay registros, no hace nada.
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
                    Nombre = "Caja 1",
                    Activo = true,
                    UltimaSyncActividades = null
                },
                new PuntoVentaSiat
                {
                    CodigoSucursal = 0,
                    CodigoPuntoVenta = 1,
                    Nombre = "Caja 2",
                    // Empieza inactivo. Se activa por UPDATE en BD cuando
                    // se quiera usar esa caja.
                    Activo = false,
                    UltimaSyncActividades = null
                }
            );

            await db.SaveChangesAsync();
        }
    }
}