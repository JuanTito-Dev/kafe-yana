using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.DataLoders
{
    public class RecetaCantidadProducibleDataLoader : BatchDataLoader<int, int>
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public RecetaCantidadProducibleDataLoader(
            IDbContextFactory<AppDbContext> dbFactory,
            IBatchScheduler scheduler,
            DataLoaderOptions options) : base(scheduler, options)
        {
            _dbFactory = dbFactory;
        }

        protected override async Task<IReadOnlyDictionary<int, int>> LoadBatchAsync(
            IReadOnlyList<int> recetaIds,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // UNA sola query para todas las recetas
            var detalles = await db.DetalleReceta
                .Where(d => recetaIds.Contains(d.Id_receta))
                .Include(d => d.Insumo)
                .ToListAsync(ct);

            var detallesPorRecetaId = detalles
                .GroupBy(d => d.Id_receta)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resultado = new Dictionary<int, int>();

            foreach (var recetaId in recetaIds)
            {
                if (!detallesPorRecetaId.TryGetValue(recetaId, out var detallesReceta) || !detallesReceta.Any())
                {
                    resultado[recetaId] = 0;
                    continue;
                }

                resultado[recetaId] = detallesReceta
                    .Select(d =>
                    {
                        if (d.Insumo == null || d.Cantidad == 0) return 0;
                        return (int)Math.Floor(
                            (double)d.Insumo.Stock_actual /
                            ((double)d.Cantidad * (1 + (double)d.Merma / 100.0))
                        );
                    })
                    .Min();
            }

            return resultado;
        }
            
    }
}
