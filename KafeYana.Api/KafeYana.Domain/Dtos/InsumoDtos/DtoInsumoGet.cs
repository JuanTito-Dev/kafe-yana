using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.InsumoDtos
{
    public class DtoInsumoGet : BaseEntity
    {
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Unidad_min_uso { get; set; } = string.Empty;
        public string Unidad_compra { get; set; } = string.Empty;
        public decimal Factor_conversion { get; set; }
        public required decimal Costo { get; set; }
        public int Stock_actual { get; set; }
        public int Stock_min { get; set; }

        public static DtoInsumoGet Desde(Insumo datos) => new DtoInsumoGet
        {
            Id = datos.Id,
            Nombre = datos.Nombre,
            Categoria = datos.Categoria,
            Unidad_min_uso = datos.Unidad_min_uso,
            Unidad_compra = datos.Unidad_compra,
            Factor_conversion = datos.Factor_conversion,
            Costo = datos.Costo,
            Stock_actual = datos.Stock_actual,
            Stock_min = datos.Stock_min
        };
    }
}
