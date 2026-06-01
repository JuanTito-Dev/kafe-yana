using KafeYana.Domain.Entities.Inventario;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.Hubs
{
    /// <summary>
    /// Construye el payload de StockActualizado a partir de una Ronda recién procesada.
    /// No modifica ningún dato; solo lee el estado post-venta de la BD.
    /// </summary>
    public class StockPayloadService(IDbContextFactory<AppDbContext> _dbFactory)
    {
        public async Task<StockActualizadoPayload> BuildAsync(Ronda ronda)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // ── Paso 1: Clasificar las líneas de la ronda ─────────────────────
            // Productos simples (comprados/elaborados vendidos directamente)
            var simpleProductIds = ronda.Detalle
                .Where(d => !d.ItemsCombo.Any())
                .Select(d => d.Id_Producto)
                .Where(id => id != 0)
                .Distinct()
                .ToList();

            // Combos vendidos en esta ronda
            var directComboProductIds = ronda.Detalle
                .Where(d => d.ItemsCombo.Any())
                .Select(d => d.Id_Producto)
                .Distinct()
                .ToList();

            // Componentes dentro de los combos
            var comboComponentIds = ronda.Detalle
                .SelectMany(d => d.ItemsCombo)
                .Select(i => i.Id_Producto)
                .Where(id => id != 0)
                .Distinct()
                .ToList();

            // Todos los IDs directamente afectados (simples + componentes de combos)
            var allDirectIds = simpleProductIds.Union(comboComponentIds).Distinct().ToList();

            // ── Paso 2: Separar comprados de elaborados ───────────────────────
            var directCompradoProductIds = (await db.Comprados
                .Where(c => allDirectIds.Contains(c.Id_Producto))
                .Select(c => c.Id_Producto)
                .ToListAsync())
                .ToHashSet();

            var directElaboradoProductIds = allDirectIds
                .Where(id => !directCompradoProductIds.Contains(id))
                .ToList();

            // ── Paso 3: Obtener todos los insumos consumidos ──────────────────
            var affectedInsumoIds = new HashSet<int>();
            if (directElaboradoProductIds.Count > 0)
            {
                var ids = await db.Elaborados
                    .Where(e => directElaboradoProductIds.Contains(e.Id_Producto) && e.Receta != null)
                    .SelectMany(e => e.Receta!.Detalles)
                    .Select(d => d.Id_insumo)
                    .Distinct()
                    .ToListAsync();

                affectedInsumoIds = ids.ToHashSet();
            }

            // ── Paso 4: Expandir a TODOS los elaborados que usan esos insumos ─
            // Esto captura otros cafés, otras bebidas, etc. que no estaban en la ronda
            // pero que comparten ingredientes con los que sí estaban.
            var allAffectedElaboradoProductIds = new HashSet<int>(directElaboradoProductIds);
            if (affectedInsumoIds.Count > 0)
            {
                var related = await db.Elaborados
                    .Where(e => e.Receta != null &&
                                e.Receta!.Detalles.Any(d => affectedInsumoIds.Contains(d.Id_insumo)))
                    .Select(e => e.Id_Producto)
                    .ToListAsync();

                foreach (var id in related)
                    allAffectedElaboradoProductIds.Add(id);
            }

            // ── Paso 5: Expandir a TODOS los combos que contienen productos afectados ─
            // Si se vendió un comprado suelto que también está en un combo,
            // el combo también se reporta para que el front actualice su disponibilidad.
            var allAffectedProductIds = directCompradoProductIds
                .Union(allAffectedElaboradoProductIds)
                .ToList();

            var allAffectedComboProductIds = new HashSet<int>(directComboProductIds);
            if (allAffectedProductIds.Count > 0)
            {
                var relatedCombos = await db.DetallePromciones
                    .Where(dp => allAffectedProductIds.Contains(dp.Id_Producto))
                    .Select(dp => dp.Promocion.Producto_Id)
                    .Distinct()
                    .ToListAsync();

                foreach (var id in relatedCombos)
                    allAffectedComboProductIds.Add(id);
            }

            // ── Paso 6: Construir el payload con el stock actualizado ──────────
            var compradosResult  = new List<CompradoStockItem>();
            var elaboradosResult = new List<ElaboradoStockItem>();
            var combosResult     = new List<ComboStockItem>();

            if (directCompradoProductIds.Count > 0)
            {
                var comprados = await db.Comprados
                    .Where(c => directCompradoProductIds.Contains(c.Id_Producto))
                    .ToListAsync();

                foreach (var c in comprados)
                    compradosResult.Add(new CompradoStockItem(c.Id_Producto, c.Stock_actual));
            }

            if (allAffectedElaboradoProductIds.Count > 0)
            {
                var elaborados = await db.Elaborados
                    .Where(e => allAffectedElaboradoProductIds.Contains(e.Id_Producto))
                    .Include(e => e.Receta)
                        .ThenInclude(r => r.Detalles)
                            .ThenInclude(d => d.Insumo)
                    .ToListAsync();

                foreach (var e in elaborados)
                {
                    if (e.Producible)
                    {
                        elaboradosResult.Add(new ElaboradoStockItem(e.Id_Producto, e.Stock_actual, null));
                    }
                    else
                    {
                        if (e.Receta is null) continue;
                        var producible = CalcularProducibleReceta(e.Receta);
                        elaboradosResult.Add(new ElaboradoStockItem(e.Id_Producto, 0, producible));
                    }
                }
            }

            if (allAffectedComboProductIds.Count > 0)
            {
                var promociones = await db.Promociones
                    .Where(p => allAffectedComboProductIds.Contains(p.Producto_Id))
                    .Include(p => p.Detalles)
                        .ThenInclude(d => d.Producto)
                            .ThenInclude(p => p.Comprado)
                    .Include(p => p.Detalles)
                        .ThenInclude(d => d.Producto)
                            .ThenInclude(p => p.Elaborado)
                                .ThenInclude(e => e.Receta)
                                    .ThenInclude(r => r.Detalles)
                                        .ThenInclude(d => d.Insumo)
                    .ToListAsync();

                foreach (var promo in promociones)
                {
                    var producible = CalcularProducibleCombo(promo.Detalles);
                    combosResult.Add(new ComboStockItem(promo.Producto_Id, producible));
                }
            }

            return new StockActualizadoPayload(compradosResult, elaboradosResult, combosResult);
        }

        public async Task<StockActualizadoPayload> BuildPorPedidoAsync(int idPedido)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var detalles = await db.Rondas
                .Where(r => r.Id_Pedido == idPedido)
                .SelectMany(r => r.Detalle)
                .Include(d => d.ItemsCombo)
                .ToListAsync();

            var rondaSintetica = new Ronda
            {
                Id_Pedido = idPedido,
                Ronda_Descripcion = string.Empty,
                SubTotal = 0,
                Detalle = detalles
            };

            return await BuildAsync(rondaSintetica);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static int CalcularProducibleReceta(Receta receta)
        {
            if (receta.Detalles is null || receta.Detalles.Count == 0 || receta.Porciones <= 0)
                return 0;

            return receta.Detalles
                .Select(d =>
                {
                    if (d.Insumo is null || d.Cantidad == 0) return 0;
                    var cantPorPorcion = (double)d.Cantidad / receta.Porciones;
                    var factor = cantPorPorcion * (1 + (double)d.Merma / 100.0);
                    return factor == 0 ? 0 : (int)Math.Floor((double)d.Insumo.Stock_actual / factor);
                })
                .Min();
        }

        private static int CalcularProducibleCombo(ICollection<PromocionDetalle> detalles)
        {
            var produciblePorComponente = new List<int>();

            foreach (var item in detalles)
            {
                if (item.Cantidad <= 0) { produciblePorComponente.Add(0); continue; }

                var producto = item.Producto;

                if (producto?.Comprado is not null)
                {
                    produciblePorComponente.Add(
                        (int)Math.Floor((double)producto.Comprado.Stock_actual / item.Cantidad)
                    );
                    continue;
                }

                if (producto?.Elaborado is not null)
                {
                    var elaborado = producto.Elaborado;

                    if (elaborado.Producible)
                    {
                        produciblePorComponente.Add(
                            (int)Math.Floor((double)elaborado.Stock_actual / item.Cantidad)
                        );
                        continue;
                    }

                    if (elaborado.Receta is null) continue;

                    var producibleReceta = CalcularProducibleReceta(elaborado.Receta);
                    produciblePorComponente.Add(
                        (int)Math.Floor((double)producibleReceta / item.Cantidad)
                    );
                }
            }

            return produciblePorComponente.Count > 0 ? produciblePorComponente.Min() : 0;
        }
    }
}
