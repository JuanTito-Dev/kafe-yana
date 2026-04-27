using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.DataLoders
{
    public class PromocionCantidadProducibleDataLoader : BatchDataLoader<int, int>
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public PromocionCantidadProducibleDataLoader(
            IDbContextFactory<AppDbContext> dbFactory,
            IBatchScheduler scheduler,
            DataLoaderOptions options) : base(scheduler, options)
        {
            _dbFactory = dbFactory;
        }

        protected override async Task<IReadOnlyDictionary<int, int>> LoadBatchAsync(
            IReadOnlyList<int> promocionIds,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // UNA query para todos los combos
            var detalles = await db.DetallePromciones
                .Where(d => promocionIds.Contains(d.Id_Promocion))
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Elaborado)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Comprado)
                .ToListAsync(ct);

            var elaboradoIds = detalles
                .Where(d => d.Producto?.Elaborado != null)
                .Select(d => d.Producto!.Elaborado!.Id)
                .Distinct()
                .ToList();

            // UNA query para todas las recetas
            var recetas = await db.Recetas
                .Where(r => elaboradoIds.Contains(r.Id_Elaborado!.Value))
                .ToListAsync(ct);

            var recetaIds = recetas.Select(r => r.Id).ToList();

            // UNA query para todos los detalles de receta
            var detallesReceta = await db.DetalleReceta
                .Where(d => recetaIds.Contains(d.Id_receta))
                .Include(d => d.Insumo)
                .ToListAsync(ct);

            var recetaPorElaboradoId = recetas
                .Where(r => r.Id_Elaborado.HasValue)
                .ToDictionary(r => r.Id_Elaborado!.Value);

            var detallesPorRecetaId = detallesReceta
                .GroupBy(d => d.Id_receta)
                .ToDictionary(g => g.Key, g => g.ToList());

            var detallesPorPromocion = detalles
                .GroupBy(d => d.Id_Promocion)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resultado = new Dictionary<int, int>();

            foreach (var promocionId in promocionIds)
            {
                if (!detallesPorPromocion.TryGetValue(promocionId, out var detallesCombo))
                {
                    resultado[promocionId] = 0;
                    continue;
                }

                var produciblesPorDetalle = new List<int>();

                foreach (var detalle in detallesCombo)
                {
                    var producto = detalle.Producto;
                    var cantidadRequerida = detalle.Cantidad;

                    if (cantidadRequerida <= 0) { produciblesPorDetalle.Add(0); continue; }

                    if (producto?.Comprado != null)
                    {
                        produciblesPorDetalle.Add(
                            (int)Math.Floor((double)producto.Comprado.Stock_actual / (double)cantidadRequerida)
                        );
                        continue;
                    }

                    if (producto?.Elaborado != null)
                    {
                        var elaborado = producto.Elaborado;

                        if (!recetaPorElaboradoId.TryGetValue(elaborado.Id, out var receta))
                            continue;

                        if (!detallesPorRecetaId.TryGetValue(receta.Id, out var dReceta) || !dReceta.Any())
                            continue;

                        int producibleDeReceta = dReceta
                            .Select(d =>
                            {
                                if (d.Insumo == null || d.Cantidad == 0) return 0;
                                return (int)Math.Floor(
                                    (double)d.Insumo.Stock_actual /
                                    ((double)d.Cantidad * (1 + (double)d.Merma / 100.0))
                                );
                            })
                            .Min();

                        produciblesPorDetalle.Add(
                            (int)Math.Floor((double)producibleDeReceta / (double)cantidadRequerida)
                        );
                        continue;
                    }
                }

                resultado[promocionId] = produciblesPorDetalle.Any() ? produciblesPorDetalle.Min() : 0;
            }

            return resultado;
        }
    }
}
