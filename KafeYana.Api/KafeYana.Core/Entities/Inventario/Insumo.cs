using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Insumo : BaseEntity
    {
        public required string Nombre { get; set; }

        public required string Categoria { get; set; }

        public required string Unidad_min_uso { get; set; }

        public required string Unidad_compra {  get; set; }

        public required decimal Factor_conversion { get; set; }

        public required decimal Costo { get; set; }

        public required int Stock_actual { get; set; }

        public required int Stock_min { get; set; }

        public ICollection<Detalle> Detalles { get; set; }

        public ICollection<Ajuste> Ajustes { get; set; }

        public ICollection<Ajuste> AjustesComoNuevo { get; set; }
    }
}
