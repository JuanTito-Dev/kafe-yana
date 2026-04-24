using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Auxiliares.Recetas
{
    public class AuxReceta
    {
        public static int CantidadProducible(Receta? receta)
        {
            if (receta == null || receta.Detalles == null || !receta.Detalles.Any())
                return 0;
            return receta.Detalles.Any()
                    ?  receta.Detalles
                        .Select(d => d.Insumo.Stock_actual == 0 || d.Cantidad == 0
                        ? 0
                        : (int)Math.Floor(
                            (double)d.Insumo.Stock_actual /
                                ((double)d.Cantidad * (1 + (double)d.Merma / 100.0)) // ← si merma es 0-100
                                ))
                            .Min()
                            : 0;
        }
    }
}
