using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Seeders
{
    public static class AceleradorPuntosSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.AceleradorPuntos.AnyAsync()) return;

            db.AceleradorPuntos.AddRange(
                new AceleradorPuntos
                {
                    Tipo           = TipoAcelerador.Combo,
                    TipoAplicacion = TipoAplicacionAcelerador.Suma,
                    Cantidad       = 2,
                    Activo         = false
                },
                new AceleradorPuntos
                {
                    Tipo           = TipoAcelerador.CompraAlta,
                    TipoAplicacion = TipoAplicacionAcelerador.Multiplicador,
                    Cantidad       = 2,
                    UmbralMonto    = 100,
                    Activo         = false
                },
                new AceleradorPuntos
                {
                    Tipo           = TipoAcelerador.CompraMediana,
                    TipoAplicacion = TipoAplicacionAcelerador.Suma,
                    Cantidad       = 2,
                    UmbralMonto    = 70,
                    Activo         = false
                },
                new AceleradorPuntos
                {
                    Tipo           = TipoAcelerador.Cumpleanos,
                    TipoAplicacion = TipoAplicacionAcelerador.Multiplicador,
                    Cantidad       = 2,
                    Activo         = false
                },
                new AceleradorPuntos
                {
                    Tipo           = TipoAcelerador.HoraValle,
                    TipoAplicacion = TipoAplicacionAcelerador.Suma,
                    Cantidad       = 2,
                    HoraInicio     = new TimeOnly(14, 0),
                    HoraFin        = new TimeOnly(17, 0),
                    Activo         = false
                }
            );

            await db.SaveChangesAsync();
        }
    }
}
