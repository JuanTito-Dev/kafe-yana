using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Core.Entities.Inventario
{
    public class Categoria
    {
        public required string Nombre { get ; set; }

        public string CategoriaPadre { get; set; } = string.Empty;

        public string DescripcionCorta {  get; set; } = string.Empty;

        public required int Orden { get; set; }

        public required bool Menu {  get; set; }

        public string Color {  get; set; } = string.Empty;
    }
}
