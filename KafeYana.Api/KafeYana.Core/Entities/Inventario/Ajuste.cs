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
        public required int Id_Opcion {  get; set; }

        public required int Id_Insumo { get; set; }

        public required int Cantidad { get; set; }

        public required string TipoAjuste { get; set; }

        public Opcion Opcion { get; set; }

        public Insumo Insumo  { get; set; }
    }
}
