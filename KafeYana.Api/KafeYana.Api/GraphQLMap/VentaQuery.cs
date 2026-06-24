using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class VentaQuery
    {
        [UsePaging(IncludeTotalCount = true)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Mesero, RolesKafe.Cajero })]

        public IQueryable<Venta> Ventas([Service] IVentaRepositorio _Venta)
        {
            return _Venta.VentaQuery();
        }

        /// <summary>
        /// KPIs agregados de ventas para las tarjetas superiores del Historial.
        /// Acepta el mismo <c>where</c> que <c>ventas</c> (fecha + estado SIAT) y
        /// calcula los totales sobre TODAS las páginas coincidentes en el backend.
        /// "Hoy" y "Mes" se evalúan en zona horaria local (America/La_Paz si está
        /// disponible, si no UTC). Se cuentan TODAS las ventas del rango (incluso
        /// las no facturadas y las con estado SIAT no-validada) — sólo el filtro
        /// <c>where</c> reduce el universo.
        /// </summary>
        [Authorize(Roles = new[] { RolesKafe.Admin, RolesKafe.Cajero })]
        public async Task<VentasEstadisticas> VentasEstadisticas(
            [Service] IVentaRepositorio _Venta,
            VentasEstadisticasFiltroInput? where,
            CancellationToken ct)
        {
            var q = _Venta.VentaQuery();

            if (where is not null)
            {
                if (where.FechaDesde.HasValue)
                    q = q.Where(v => v.FechaEmision >= where.FechaDesde.Value);
                if (where.FechaHasta.HasValue)
                    q = q.Where(v => v.FechaEmision <= where.FechaHasta.Value);
                if (where.EstadoSiat.HasValue)
                    q = q.Where(v => v.EstadoSiat == where.EstadoSiat.Value);
            }

            // Sin filtro de estado SIAT aquí a propósito: el usuario pidió
            // contar TODAS las ventas que coincidan con el `where`, incluyendo
            // las que no fueron facturadas electrónicamente o están observadas/
            // pendientes. Sólo se respeta el `where` recibido.

            // Ventana temporal "hoy" / "mes en curso" en zona horaria local.
            // Venta.FechaEmision está en UTC, así que convertimos desde la TZ local.
            var tz = ResolveLaPazTimeZone();
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var startOfTodayLocal = nowLocal.Date;
            var startOfMonthLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1);
            var startOfTodayUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(startOfTodayLocal, DateTimeKind.Unspecified), tz);
            var startOfMonthUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(startOfMonthLocal, DateTimeKind.Unspecified), tz);

            var baseQuery = q.Where(v => v.FechaEmision >= startOfMonthUtc);
            var totalMes = await baseQuery.SumAsync(v => (decimal?)v.MontoTotal, ct) ?? 0m;
            var conteoMes = await baseQuery.CountAsync(ct);

            var hoyQuery = baseQuery.Where(v => v.FechaEmision >= startOfTodayUtc);
            var totalHoy = await hoyQuery.SumAsync(v => (decimal?)v.MontoTotal, ct) ?? 0m;
            var conteoHoy = await hoyQuery.CountAsync(ct);

            var ticketPromedioMes = conteoMes > 0 ? totalMes / conteoMes : 0m;

            return new VentasEstadisticas
            {
                TotalHoy = totalHoy,
                TotalMes = totalMes,
                ConteoHoy = conteoHoy,
                ConteoMes = conteoMes,
                TicketPromedioMes = ticketPromedioMes,
            };
        }

        private static TimeZoneInfo ResolveLaPazTimeZone()
        {
            // Linux/macOS usan el id IANA; Windows usa el id "SA Western Standard Time".
            // Si ninguno está disponible caemos a UTC (incorrecto para "hoy" pero
            // no rompe la página — es sólo un KPI orientativo).
            try { return TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz"); }
            catch { /* fallthrough */ }
            try { return TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time"); }
            catch { return TimeZoneInfo.Utc; }
        }
    }

    /// <summary>
    /// Filtro aceptado por <c>ventasEstadisticas</c>. Reutiliza los mismos rangos
    /// de fecha y estado SIAT que <c>ventas(where:)</c> pero ignora la búsqueda
    /// textual (no aplica a KPIs agregados).
    /// </summary>
    public class VentasEstadisticasFiltroInput
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public FacturaEstado? EstadoSiat { get; set; }
    }
}
