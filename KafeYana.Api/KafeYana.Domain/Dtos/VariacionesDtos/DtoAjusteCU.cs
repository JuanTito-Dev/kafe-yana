using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.VariacionesDtos
{
    public class DtoAjusteCU
    {
        public required int Id_Insumo { get; set; }

        public int? Id_InsumoNuevo { get; set; }

        public required decimal Cantidad { get; set; } 
    }
}
