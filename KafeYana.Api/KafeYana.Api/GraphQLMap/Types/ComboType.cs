using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.GraphQLMap.Types
{
    public class ComboType : ObjectType<Promocion>
    {
        protected override void Configure(IObjectTypeDescriptor<Promocion> descriptor)
        {
            descriptor.Field(x => x.Producto).Type<ProductoType>();
            descriptor.Field(x => x.Id).IsProjected(true); ;      // interno, no visible en schema
            descriptor.Field(x => x.Producto_Id).IsProjected(true); // interno, no visible en schema
            descriptor.Field(x => x.Detalles).Type<ListType<PromocionDetalleType>>();
            descriptor.Field("cantidadProducible").Type<IntType>()
            .Resolve(async ctx =>
            {
                

                var promocion = ctx.Parent<Promocion>();
                var db = ctx.Service<AppDbContext>();

                Console.WriteLine($"promocion.Id = {promocion.Id}");

                var detalles = await db.DetallePromciones
                    .Where(d => d.Id_Promocion == promocion.Id)
                    .Include(d => d.Producto)
                        .ThenInclude(p => p.Elaborado)
                    .Include(d => d.Producto)
                        .ThenInclude(p => p.Comprado)
                    .ToListAsync();

                if (!detalles.Any()) return 0;

                // IDs de Elaborados con Producible = false
                var elaboradoIds = detalles
                    .Where(d => d.Producto?.Elaborado != null && !d.Producto.Elaborado.Producible)
                    .Select(d => d.Producto!.Elaborado!.Id)  // ← ID del Elaborado
                    .ToList();

                // Traer todas las recetas de esos elaborados de una sola vez
                var recetas = await db.Recetas
                    .Where(r => elaboradoIds.Contains(r.Id_Elaborado!.Value))
                    .ToListAsync();

                var recetaIds = recetas.Select(r => r.Id).ToList();

                // Traer todos los detalles de esas recetas de una sola vez
                var todosDetallesReceta = await db.DetalleReceta
                    .Where(d => recetaIds.Contains(d.Id_receta))
                    .Include(d => d.Insumo)
                    .ToListAsync();

                // Agrupar para acceso rápido — key es Id del Elaborado
                var recetaPorElaboradoId = recetas
                    .Where(r => r.Id_Elaborado.HasValue)
                    .ToDictionary(r => r.Id_Elaborado!.Value);

                var detallesPorRecetaId = todosDetallesReceta
                    .GroupBy(d => d.Id_receta)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var produciblesPorDetalle = new List<int>();

                foreach (var detalle in detalles)
                {
                    var producto = detalle.Producto;
                    var cantidadRequerida = detalle.Cantidad;



                    Console.WriteLine($"=== Detalle: {producto?.Id} - {cantidadRequerida} ===");
                    Console.WriteLine($"Comprado: {producto?.Comprado != null}");
                    Console.WriteLine($"Elaborado: {producto?.Elaborado != null}");
                    if (producto?.Elaborado != null)
                        Console.WriteLine($"Producible: {producto.Elaborado.Producible} | Stock: {producto.Elaborado.Stock_actual}");
                    if (producto?.Comprado != null)
                        Console.WriteLine($"Stock comprado: {producto.Comprado.Stock_actual}");



                    if (cantidadRequerida <= 0) { produciblesPorDetalle.Add(0); continue; }

                    // Comprado
                    if (producto?.Comprado != null)
                    {
                        produciblesPorDetalle.Add(
                            (int)Math.Floor((double)producto.Comprado.Stock_actual / (double)cantidadRequerida)
                        );
                        continue;
                    }

                    // Elaborado
                    if (producto?.Elaborado != null)
                    {
                        var elaborado = producto.Elaborado;

                        if (elaborado.Producible)
                        {
                            produciblesPorDetalle.Add(
                                (int)Math.Floor((double)elaborado.Stock_actual / (double)cantidadRequerida)
                            );
                            continue;
                        }

                        // Producible = false → desde receta (ya en memoria)
                        if (!recetaPorElaboradoId.TryGetValue(elaborado.Id, out var receta))
                        {
                            continue; // Sin receta → ignorar
                        }

                        if (!detallesPorRecetaId.TryGetValue(receta.Id, out var detallesReceta) || !detallesReceta.Any())
                        {
                            continue; // Receta sin detalles → ignorar
                        }

                        int producibleDeReceta = detallesReceta
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

                    // Sin Elaborado ni Comprado → ignorar
                    continue;
                }

                return produciblesPorDetalle.Any() ? produciblesPorDetalle.Min() : 0;
            });
        }
    }
}
