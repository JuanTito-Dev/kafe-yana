using KafeYana.Application.Dtos.ReporteDtos;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Servicios
{
    /// <summary>
    /// Reporte mensual de productos más vendidos / mayor rotación.
    ///
    /// Fuente de ventas: <c>Venta</c> con EstadoSiat == null (sin factura) o
    /// EstadoSiat ∈ {Validada, Observada} (excluye Anulada, Pendiente, Rechazada)
    /// en el rango del mes calendario.
    /// Las líneas de producto vienen de <c>Detalle_Pago.Descripcion</c> (mismo
    /// approach que el Top 10 ya implementado en <c>SalesReportPage</c>).
    ///
    /// Stock: para reconstruir el stock al INICIO del mes se consulta
    /// <c>ProductoMovimiento.Stock_resultante</c> — el último registro anterior o
    /// igual al inicio del mes. Si no existe ninguno, se usa el stock ACTUAL como
    /// fallback (asumiendo que no cambió durante el mes).
    ///
    /// Rotación = UnidadesVendidas / StockPromedio, donde
    /// StockPromedio = (StockInicioMes + StockFinMes) / 2.
    /// </summary>
    public class ReporteProductosService(AppDbContext _db)
    {
        public async Task<DtoReporteMensualProductos> GenerarReporteMensualAsync(
            int? mes,
            int? anio,
            CancellationToken ct = default)
        {
            var tz = ResolveLaPazTimeZone();
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            int mesFinal = mes ?? nowLocal.Month;
            int anioFinal = anio ?? nowLocal.Year;

            var inicioLocal = new DateTime(anioFinal, mesFinal, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var finLocal = inicioLocal.AddMonths(1);

            var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, tz);
            var finUtc = TimeZoneInfo.ConvertTimeToUtc(finLocal, tz);

            // ── Ventas del mes + sus detalles ─────────────────────────────────────
            // Agrupamos por descripción de Detalle_Pago (clave de producto en la factura).
            // Incluye ventas sin factura (EstadoSiat == null) además de
            // EstadoSiat ∈ {Validada, Observada} → excluye solo Anulada, Pendiente, Rechazada.
            var ventasQuery = _db.Ventas
                .AsNoTracking()
                .Where(v => v.FechaEmision >= inicioUtc && v.FechaEmision < finUtc)
                .Where(v => v.EstadoSiat == null
                         || v.EstadoSiat == FacturaEstado.Validada
                         || v.EstadoSiat == FacturaEstado.Observada);

            var lineas = await ventasQuery
                .SelectMany(v => v.Detalles)
                .Select(d => new
                {
                    d.Descripcion,
                    d.Cantidad,
                    d.SubTotal,
                    d.PrecioUnitario,
                    d.Id_venta,
                })
                .ToListAsync(ct);

            // Aggregación en memoria (las descripciones vienen como texto)
            var productos = lineas
                .GroupBy(l => l.Descripcion)
                .Select(g => new
                {
                    Descripcion = g.Key,
                    Unidades = (int)g.Sum(x => x.Cantidad),
                    Ingresos = g.Sum(x => x.SubTotal),
                    NumeroVentas = g.Select(x => x.Id_venta).Distinct().Count(),
                    PrecioPromedio = g.Average(x => x.PrecioUnitario),
                })
                .OrderByDescending(p => p.Unidades)
                .ToList();

            // KPIs
            var totalFacturado = await ventasQuery.SumAsync(v => (decimal?)v.MontoTotal, ct) ?? 0m;
            var numeroFacturas = await ventasQuery.CountAsync(ct);
            var unidadesVendidas = productos.Sum(p => p.Unidades);
            var productosDistintos = productos.Count;

            // ── Catálogo de productos → datos para los top ───────────────────────
            // Mapeamos cada descripción (clave usada en facturas) a Producto/Categoría.
            // Truco: NO hay FK directa entre Detalle_Pago y Producto (es snapshot).
            // El match se hace por nombre, normalizando mayúsculas/espacios.
            // Si no se encuentra match, igual conservamos el producto "anónimo" pero
            // sin datos de stock → rotación indefinida (0).
            var catalogo = await _db.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p => p.Tipo == TiposProductos.Comprado
                         || p.Tipo == TiposProductos.Elaborado
                         || p.Tipo == TiposProductos.Promocion)
                .Where(p => !string.IsNullOrEmpty(p.Nombre))
                .ToListAsync(ct);

            static string Norm(string s) => (s ?? string.Empty).Trim().ToLowerInvariant();
            var catalogoPorDesc = catalogo
                .GroupBy(p => Norm(p.Nombre))
                .ToDictionary(g => g.Key, g => g.First());

            // ── Snapshots de stock inicio/fin del mes ────────────────────────────
            // Para cada producto: el ÚLTIMO ProductoMovimiento.Fecha <= inicioUtc da
            // el stock al iniciar el mes (si no hay ninguno → null).
            // El stock al final del mes = stockActual del Comprado/Elaborado.
            var idsCatalogo = catalogo.Select(p => p.Id).ToList();

            var stocksFinMes = await ConstruirStockFinMesAsync(idsCatalogo, ct);

            var stocksInicioMes = await _db.Movimientos_Producto
                .AsNoTracking()
                .Where(m => idsCatalogo.Contains(m.Id_Producto) && m.Fecha < inicioUtc)
                .GroupBy(m => m.Id_Producto)
                .Select(g => new { Id_Producto = g.Key, UltimaFecha = g.Max(x => x.Fecha) })
                .ToListAsync(ct);

            var stocksInicioDetalle = await _db.Movimientos_Producto
                .AsNoTracking()
                .Where(m => idsCatalogo.Contains(m.Id_Producto) && m.Fecha < inicioUtc)
                .Where(m => stocksInicioMes.Select(s => s.Id_Producto).Contains(m.Id_Producto))
                .Select(m => new { m.Id_Producto, m.Fecha, m.Stock_resultante })
                .ToListAsync(ct);

            Dictionary<int, int?> stockInicioPorProducto = stocksInicioMes.ToDictionary(
                s => s.Id_Producto,
                _ => (int?)0);

            foreach (var grupo in stocksInicioDetalle.GroupBy(x => x.Id_Producto))
            {
                var ultimo = grupo.OrderByDescending(x => x.Fecha).First();
                stockInicioPorProducto[grupo.Key] = ultimo.Stock_resultante;
            }

            // ── Top items ────────────────────────────────────────────────────────
            var topUnidades = productos
                .Select(p =>
                {
                    catalogoPorDesc.TryGetValue(Norm(p.Descripcion), out var prod);
                    int stockFin = 0;
                    decimal stockInicio = 0m;
                    if (prod is not null)
                    {
                        stocksFinMes.TryGetValue(prod.Id, out stockFin);
                        if (stockInicioPorProducto.TryGetValue(prod.Id, out var sIni) && sIni.HasValue)
                            stockInicio = sIni.Value;
                        else
                            stockInicio = stockFin;
                    }
                    var stockPromedio = (stockInicio + stockFin) / 2m;
                    var rotacion = stockPromedio > 0 ? Math.Round(p.Unidades / stockPromedio, 4) : 0m;

                    return new DtoProductoTop
                    {
                        IdProducto = prod?.Id ?? 0,
                        Codigo = prod?.Codigo ?? string.Empty,
                        Nombre = p.Descripcion,
                        Categoria = prod?.Categoria?.Nombre ?? "Sin categoría",
                        UnidadesVendidas = p.Unidades,
                        Ingresos = Math.Round(p.Ingresos, 2),
                        PrecioPromedio = Math.Round(p.PrecioPromedio, 2),
                        NumeroVentas = p.NumeroVentas,
                        StockFinMes = stockFin,
                        StockInicioMes = (int)stockInicio,
                        StockPromedio = Math.Round(stockPromedio, 2),
                        Rotacion = rotacion,
                    };
                })
                .OrderByDescending(x => x.UnidadesVendidas)
                .Take(10)
                .ToList();

            var topIngresos = productos
                .Select(p =>
                {
                    catalogoPorDesc.TryGetValue(Norm(p.Descripcion), out var prod);
                    int stockFin = 0;
                    decimal stockInicio = 0m;
                    if (prod is not null)
                    {
                        stocksFinMes.TryGetValue(prod.Id, out stockFin);
                        if (stockInicioPorProducto.TryGetValue(prod.Id, out var sIni) && sIni.HasValue)
                            stockInicio = sIni.Value;
                        else
                            stockInicio = stockFin;
                    }
                    var stockPromedio = (stockInicio + stockFin) / 2m;
                    var rotacion = stockPromedio > 0 ? Math.Round(p.Unidades / stockPromedio, 4) : 0m;

                    return new DtoProductoTop
                    {
                        IdProducto = prod?.Id ?? 0,
                        Codigo = prod?.Codigo ?? string.Empty,
                        Nombre = p.Descripcion,
                        Categoria = prod?.Categoria?.Nombre ?? "Sin categoría",
                        UnidadesVendidas = p.Unidades,
                        Ingresos = Math.Round(p.Ingresos, 2),
                        PrecioPromedio = Math.Round(p.PrecioPromedio, 2),
                        NumeroVentas = p.NumeroVentas,
                        StockFinMes = stockFin,
                        StockInicioMes = (int)stockInicio,
                        StockPromedio = Math.Round(stockPromedio, 2),
                        Rotacion = rotacion,
                    };
                })
                .OrderByDescending(x => x.Ingresos)
                .Take(10)
                .ToList();

            // Para rotación tienen que tener un producto matcheado (sino rotacion = 0 siempre).
            var topRotacion = productos
                .Where(p => catalogoPorDesc.ContainsKey(Norm(p.Descripcion)))
                .Select(p =>
                {
                    catalogoPorDesc.TryGetValue(Norm(p.Descripcion), out var prod);
                    int stockFin = 0;
                    decimal stockInicio = 0m;
                    if (prod is not null)
                    {
                        stocksFinMes.TryGetValue(prod.Id, out stockFin);
                        if (stockInicioPorProducto.TryGetValue(prod.Id, out var sIni) && sIni.HasValue)
                            stockInicio = sIni.Value;
                        else
                            stockInicio = stockFin;
                    }
                    var stockPromedio = (stockInicio + stockFin) / 2m;
                    var rotacion = stockPromedio > 0 ? Math.Round(p.Unidades / stockPromedio, 4) : 0m;

                    return new DtoProductoTop
                    {
                        IdProducto = prod!.Id,
                        Codigo = prod.Codigo,
                        Nombre = p.Descripcion,
                        Categoria = prod.Categoria?.Nombre ?? "Sin categoría",
                        UnidadesVendidas = p.Unidades,
                        Ingresos = Math.Round(p.Ingresos, 2),
                        PrecioPromedio = Math.Round(p.PrecioPromedio, 2),
                        NumeroVentas = p.NumeroVentas,
                        StockFinMes = stockFin,
                        StockInicioMes = (int)stockInicio,
                        StockPromedio = Math.Round(stockPromedio, 2),
                        Rotacion = rotacion,
                    };
                })
                .OrderByDescending(x => x.Rotacion)
                .Take(10)
                .ToList();

            // ── Por categoría ────────────────────────────────────────────────────
            var porCategoria = productos
                .Select(p =>
                {
                    catalogoPorDesc.TryGetValue(Norm(p.Descripcion), out var prod);
                    return new
                    {
                        Categoria = prod?.Categoria?.Nombre ?? "Sin categoría",
                        Unidades = p.Unidades,
                        Ingresos = p.Ingresos,
                        Producto = prod?.Nombre ?? p.Descripcion,
                    };
                })
                .GroupBy(x => x.Categoria)
                .Select(g => new DtoProductoPorCategoria
                {
                    Categoria = g.Key,
                    UnidadesVendidas = g.Sum(x => x.Unidades),
                    Ingresos = Math.Round(g.Sum(x => x.Ingresos), 2),
                    ProductosDistintos = g.Select(x => x.Producto).Distinct().Count(),
                })
                .OrderByDescending(c => c.UnidadesVendidas)
                .ToList();

            return new DtoReporteMensualProductos
            {
                Mes = mesFinal,
                Anio = anioFinal,
                GeneradoEn = DateTime.UtcNow,
                Resumen = new DtoResumenMensualProductos
                {
                    TotalFacturado = Math.Round(totalFacturado, 2),
                    NumeroFacturas = numeroFacturas,
                    UnidadesVendidas = unidadesVendidas,
                    ProductosDistintos = productosDistintos,
                    CategoriasActivas = porCategoria.Count,
                },
                TopUnidades = topUnidades,
                TopIngresos = topIngresos,
                TopRotacion = topRotacion,
                PorCategoria = porCategoria,
            };
        }

        /// <summary>
        /// Stock actual por producto (considerando Comprado y Elaborado).
        /// Las promociones no tienen stock físico → 0.
        /// </summary>
        private async Task<Dictionary<int, int>> ConstruirStockFinMesAsync(
            IReadOnlyCollection<int> idsCatalogo,
            CancellationToken ct)
        {
            var dict = new Dictionary<int, int>();

            var comprados = await _db.Comprados
                .AsNoTracking()
                .Where(c => idsCatalogo.Contains(c.Id_Producto))
                .Select(c => new { c.Id_Producto, c.Stock_actual })
                .ToListAsync(ct);
            foreach (var c in comprados)
                dict[c.Id_Producto] = c.Stock_actual;

            var elaborados = await _db.Elaborados
                .AsNoTracking()
                .Where(e => idsCatalogo.Contains(e.Id_Producto))
                .Select(e => new { e.Id_Producto, e.Stock_actual })
                .ToListAsync(ct);
            foreach (var e in elaborados)
                dict[e.Id_Producto] = e.Stock_actual;

            return dict;
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
