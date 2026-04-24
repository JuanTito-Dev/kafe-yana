using KafeYana.Domain.Entities.Inventario;

namespace KafeYana.Api.GraphQLMap.Extencions
{
    [ExtendObjectType(typeof(Producto))]
    public class ElaboradoExtencion
    {
        public int GetCantidadProducible([Parent] Producto producto)
        {
            var detalles = producto.Elaborado.Receta?.Detalles;
            if (detalles == null || !detalles.Any()) return 0;

            return detalles
                .Select(d =>
                {
                    decimal ajustada = d.Cantidad * (1 + d.Merma);
                    return ajustada > 0
                        ? (int)Math.Floor(d.Insumo.Stock_actual / ajustada)
                        : int.MaxValue;
                })
                .Min();
        }
    }
}
