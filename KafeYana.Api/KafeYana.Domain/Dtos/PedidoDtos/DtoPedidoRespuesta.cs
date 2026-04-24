using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.PedidoDtos
{
    public class DtoPedidoRespuesta
    {

        public int? Id_Cliente { get; set; }

        public decimal Total { get; set; } = 0.00M;
    }
}
