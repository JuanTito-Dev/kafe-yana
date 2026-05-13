using KafeYana.Application.Dtos.ReporteDtos;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Servicios
{
    public class ReporteInventarioService(AppDbContext _db)
    {
        public async Task<DtoReporteInventario> GenerarAsync()
        {
            var reporte = new DtoReporteInventario
            {
                GeneradoEn = DateTime.UtcNow,
                Resumen            = await BuildResumenAsync(),
                DistribucionPorCategoria = await BuildDistribucionAsync(),
                StockBajoCritico   = await BuildStockBajoCriticoAsync(),
                ProximosAgotarse   = await BuildProximosAgotarseAsync(),
            };

            return reporte;
        }

        // ─── Resumen ────────────────────────────────────────────────────────────

        private async Task<DtoResumenInventario> BuildResumenAsync()
        {
            var totalProductos = await _db.Comprados.CountAsync(x => x.Disponible);

            var totalInsumos = await _db.Insumos.CountAsync();

            // Stock bajo: comprados con stock <= mínimo + insumos con stock <= mínimo
            var compradosBajos = await _db.Comprados
                .CountAsync(x => x.Disponible && x.Stock_actual <= x.Stock_minimo);

            var insumosBajos = await _db.Insumos
                .CountAsync(x => x.Stock_actual <= x.Stock_min);

            // Valor inventario: comprados (stock * costo) + insumos (stock * costo / factor)
            var valorComprados = await _db.Comprados
                .Where(x => x.Disponible)
                .SumAsync(x => (decimal)x.Stock_actual * x.Costo_compra);

            var valorInsumos = await _db.Insumos
                .SumAsync(x => x.Factor_conversion > 0
                    ? (decimal)x.Stock_actual * x.Costo / x.Factor_conversion
                    : (decimal)x.Stock_actual * x.Costo);

            return new DtoResumenInventario
            {
                TotalProductos      = totalProductos,
                TotalInsumos        = totalInsumos,
                ItemsConStockBajo   = compradosBajos + insumosBajos,
                ValorInventario     = Math.Round(valorComprados + valorInsumos, 2),
            };
        }

        // ─── Distribución por categoría ─────────────────────────────────────────

        private async Task<List<DtoCategoriaProdutos>> BuildDistribucionAsync()
        {
            return await _db.Productos
                .Where(x => x.Tipo == TiposProductos.Comprado || x.Tipo == TiposProductos.Elaborado)
                .GroupBy(x => x.Categoria.Nombre)
                .Select(g => new DtoCategoriaProdutos
                {
                    Categoria      = g.Key,
                    TotalProductos = g.Count(),
                })
                .OrderByDescending(x => x.TotalProductos)
                .ToListAsync();
        }

        // ─── Stock bajo crítico ──────────────────────────────────────────────────

        private async Task<List<DtoItemStockBajo>> BuildStockBajoCriticoAsync()
        {
            var compradosBajos = await _db.Comprados
                .Include(x => x.Producto).ThenInclude(p => p.Categoria)
                .Where(x => x.Disponible && x.Stock_actual <= x.Stock_minimo)
                .Select(x => new DtoItemStockBajo
                {
                    Tipo      = "Comprado",
                    Nombre    = x.Producto.Nombre,
                    Categoria = x.Producto.Categoria.Nombre,
                    Stock     = $"{x.Stock_actual} {x.Unidad_medida}",
                    Minimo    = $"{x.Stock_minimo} {x.Unidad_medida}",
                    Ratio     = x.Stock_minimo > 0
                        ? (int)Math.Round((decimal)x.Stock_actual / x.Stock_minimo * 100)
                        : 0,
                })
                .ToListAsync();

            var insumosBajos = await _db.Insumos
                .Where(x => x.Stock_actual <= x.Stock_min)
                .Select(x => new DtoItemStockBajo
                {
                    Tipo      = "Insumo",
                    Nombre    = x.Nombre,
                    Categoria = x.Categoria,
                    Stock     = $"{x.Stock_actual} {x.Unidad_min_uso}",
                    Minimo    = $"{x.Stock_min} {x.Unidad_min_uso}",
                    Ratio     = x.Stock_min > 0
                        ? (int)Math.Round((decimal)x.Stock_actual / x.Stock_min * 100)
                        : 0,
                })
                .ToListAsync();

            return compradosBajos
                .Concat(insumosBajos)
                .OrderBy(x => x.Ratio)
                .ToList();
        }

        // ─── Top 10 próximos a agotarse ──────────────────────────────────────────

        private async Task<List<DtoItemStockBajo>> BuildProximosAgotarseAsync()
        {
            var comprados = await _db.Comprados
                .Include(x => x.Producto).ThenInclude(p => p.Categoria)
                .Where(x => x.Disponible && x.Stock_minimo > 0)
                .Select(x => new DtoItemStockBajo
                {
                    Tipo      = "Comprado",
                    Nombre    = x.Producto.Nombre,
                    Categoria = x.Producto.Categoria.Nombre,
                    Stock     = $"{x.Stock_actual} {x.Unidad_medida}",
                    Minimo    = $"{x.Stock_minimo} {x.Unidad_medida}",
                    Ratio     = (int)Math.Round((decimal)x.Stock_actual / x.Stock_minimo * 100),
                })
                .ToListAsync();

            var insumos = await _db.Insumos
                .Where(x => x.Stock_min > 0)
                .Select(x => new DtoItemStockBajo
                {
                    Tipo      = "Insumo",
                    Nombre    = x.Nombre,
                    Categoria = x.Categoria,
                    Stock     = $"{x.Stock_actual} {x.Unidad_min_uso}",
                    Minimo    = $"{x.Stock_min} {x.Unidad_min_uso}",
                    Ratio     = (int)Math.Round((decimal)x.Stock_actual / x.Stock_min * 100),
                })
                .ToListAsync();

            return comprados
                .Concat(insumos)
                .OrderBy(x => x.Ratio)
                .Take(10)
                .ToList();
        }
    }
}
