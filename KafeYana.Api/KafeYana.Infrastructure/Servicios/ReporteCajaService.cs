using KafeYana.Application.Dtos.ReporteDtos;
using KafeYana.Domain.Entities;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Servicios
{
    /// <summary>
    /// Genera el reporte diario de caja: lista TODAS las sesiones cuya apertura
    /// cae en la fecha indicada (TZ La Paz), incluyendo la Caja activa si existe.
    /// Pensado para vincularse desde el KPICard "Cajas Abiertas" del Dashboard.
    /// </summary>
    public class ReporteCajaService(AppDbContext _db)
    {
        public async Task<DtoReporteDiarioCaja> GenerarReporteDiarioAsync(
            DateTime fechaLocal,
            CancellationToken ct = default)
        {
            // Normalizamos a la TZ La Paz para "abrir" el día calendario local.
            var tz = ResolveLaPazTimeZone();
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var baseDate = fechaLocal.Date == default
                ? nowLocal.Date
                : fechaLocal.Date;

            var inicioLocal = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var finLocal = inicioLocal.AddDays(1);

            var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, tz);
            var finUtc = TimeZoneInfo.ConvertTimeToUtc(finLocal, tz);

            // ── Sesiones CERRADAS en el día ───────────────────────────────────────
            var historiales = await _db.CajaHistorial
                .AsNoTracking()
                .Where(h => h.Apertura >= inicioUtc && h.Apertura < finUtc)
                .OrderBy(h => h.Apertura)
                .ToListAsync(ct);

            // Movimientos de las sesiones cerradas (snapshot por sesión)
            var ids = historiales.Select(h => h.Id).ToList();
            var movimientosCerrados = await _db.CajaHistorialMovimientos
                .AsNoTracking()
                .Where(m => ids.Contains(m.Id_CajaHistorial))
                .ToListAsync(ct);

            var movsPorCerrado = movimientosCerrados
                .GroupBy(m => m.Id_CajaHistorial)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ── Sesión ACTIVA (si existe) ────────────────────────────────────────
            // La incluimos sólo si su apertura cae hoy; no tendría sentido mostrarla
            // en un día histórico donde ya no existe.
            var cajaActiva = await _db.Cajas
                .AsNoTracking()
                .Include(c => c.Movimientos)
                .FirstOrDefaultAsync(c => c.Abierta, ct);

            CajaHistorial? activaComoHistorial = null;
            List<CajaMovimiento> movsActiva = new();
            if (cajaActiva is not null && cajaActiva.FechaApertura >= inicioUtc && cajaActiva.FechaApertura < finUtc)
            {
                activaComoHistorial = new CajaHistorial
                {
                    Id = cajaActiva.Id,
                    Codigo = cajaActiva.Nombre,
                    Apertura = cajaActiva.FechaApertura,
                    Cierre = cajaActiva.FechaCierre ?? DateTime.UtcNow,
                    SaldoInicial = cajaActiva.SaldoInicial,
                    TotalIngresos = cajaActiva.TotalIngresos,
                    TotalEgresos = cajaActiva.TotalEgresos,
                    TotalVentas = cajaActiva.TotalVentas,
                    TotalEfectivo = cajaActiva.TotalEfectivo,
                    TotalTarjeta = cajaActiva.TotalTarjeta,
                    TotalQr = cajaActiva.TotalQr,
                    Diferencia = cajaActiva.SaldoEsperado - (cajaActiva.SaldoInicial + cajaActiva.TotalEfectivo + cajaActiva.TotalIngresos + cajaActiva.TotalEgresos),
                    Estado = "Abierta",
                    AbiertaPor = cajaActiva.AbiertaPor,
                    CerradaPor = null,
                };

                // Solo movimientos del día mostrado (una caja activa podría sobrevivir varios días)
                movsActiva = cajaActiva.Movimientos
                    .Where(m => m.Fecha >= inicioUtc && m.Fecha < finUtc)
                    .OrderBy(m => m.Fecha)
                    .ToList();
            }

            // ── Construcción de DTOs ─────────────────────────────────────────────
            var cajasDto = new List<DtoCajaDelDia>();
            var movsDto = new List<DtoMovimientoCajaDelDia>();

            foreach (var h in historiales)
            {
                var lista = movsPorCerrado.TryGetValue(h.Id, out var l) ? l : new List<CajaHistorialMovimiento>();
                cajasDto.Add(new DtoCajaDelDia
                {
                    Id = h.Id,
                    Codigo = h.Codigo,
                    Apertura = h.Apertura,
                    Cierre = h.Cierre,
                    AbiertaPor = h.AbiertaPor,
                    CerradaPor = h.CerradaPor,
                    SaldoInicial = h.SaldoInicial,
                    TotalVentas = h.TotalVentas,
                    TotalEfectivo = h.TotalEfectivo,
                    TotalTarjeta = h.TotalTarjeta,
                    TotalQr = h.TotalQr,
                    TotalIngresos = h.TotalIngresos,
                    TotalEgresos = Math.Abs(h.TotalEgresos), // persistido como negativo
                    Diferencia = h.Diferencia,
                    Estado = h.Estado,
                    AbiertaActualmente = false,
                    Movimientos = lista.Select(m => new DtoMovimientoCajaDelDia
                    {
                        Id = m.Id,
                        CodigoCaja = h.Codigo,
                        Fecha = h.Cierre,
                        Tipo = m.Tipo,
                        Categoria = m.Categoria,
                        Descripcion = m.Descripcion,
                        Monto = m.Monto,
                    }).ToList(),
                });

                movsDto.AddRange(cajasDto.Last().Movimientos);
            }

            if (activaComoHistorial is not null)
            {
                cajasDto.Add(new DtoCajaDelDia
                {
                    Id = activaComoHistorial.Id,
                    Codigo = activaComoHistorial.Codigo,
                    Apertura = activaComoHistorial.Apertura,
                    Cierre = null,
                    AbiertaPor = activaComoHistorial.AbiertaPor,
                    CerradaPor = null,
                    SaldoInicial = activaComoHistorial.SaldoInicial,
                    TotalVentas = cajaActiva!.TotalVentas,
                    TotalEfectivo = cajaActiva.TotalEfectivo,
                    TotalTarjeta = cajaActiva.TotalTarjeta,
                    TotalQr = cajaActiva.TotalQr,
                    TotalIngresos = cajaActiva.TotalIngresos,
                    TotalEgresos = Math.Abs(cajaActiva.TotalEgresos),
                    Diferencia = activaComoHistorial.Diferencia,
                    Estado = "Abierta",
                    AbiertaActualmente = true,
                    Movimientos = movsActiva.Select(m => new DtoMovimientoCajaDelDia
                    {
                        Id = m.Id,
                        CodigoCaja = cajaActiva.Nombre,
                        Fecha = m.Fecha,
                        Tipo = m.Tipo,
                        Categoria = m.Categoria,
                        Descripcion = m.Descripcion,
                        Monto = m.Monto,
                        Referencia = m.Referencia,
                        Nota = m.Nota,
                    }).ToList(),
                });

                movsDto.AddRange(cajasDto.Last().Movimientos);
            }

            cajasDto = cajasDto.OrderBy(c => c.Apertura).ToList();

            var resumen = new DtoResumenDiarioCaja
            {
                CajasIniciadas = cajasDto.Count,
                CajasCerradas = cajasDto.Count(c => !c.AbiertaActualmente),
                HayCajaAbierta = cajasDto.Any(c => c.AbiertaActualmente),
                TotalVentas = cajasDto.Sum(c => c.TotalVentas),
                TotalEfectivo = cajasDto.Sum(c => c.TotalEfectivo),
                TotalTarjeta = cajasDto.Sum(c => c.TotalTarjeta),
                TotalQr = cajasDto.Sum(c => c.TotalQr),
                TotalIngresos = cajasDto.Sum(c => c.TotalIngresos),
                TotalEgresos = cajasDto.Sum(c => c.TotalEgresos),
                SaldoInicialTotal = cajasDto.Sum(c => c.SaldoInicial),
                BalanceNeto = cajasDto.Sum(c => c.TotalVentas + c.TotalIngresos - c.TotalEgresos),
            };

            return new DtoReporteDiarioCaja
            {
                Fecha = baseDate,
                GeneradoEn = DateTime.UtcNow,
                Resumen = resumen,
                Cajas = cajasDto,
                Movimientos = movsDto.OrderBy(m => m.Fecha).ToList(),
            };
        }

        private static TimeZoneInfo ResolveLaPazTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz"); }
            catch { /* fallthrough */ }
            try { return TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time"); }
            catch { return TimeZoneInfo.Utc; }
        }
    }
}
