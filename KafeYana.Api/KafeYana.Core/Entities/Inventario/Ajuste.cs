using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities.Inventario
{
    public class Ajuste 
    {
        public int Id_Opcion {  get; set; }

        public required int Id_Insumo { get; set; }

        public int? Id_InsumoNuevo { get; set; }

        public required decimal Cantidad { get; set; }

        public required string TipoAjuste { get; set; }

        public Opcion Opcion { get; set; }

        public Insumo InsumoBase  { get; set; }

        public Insumo InsumoNuevo   { get; set; }
    }
}
