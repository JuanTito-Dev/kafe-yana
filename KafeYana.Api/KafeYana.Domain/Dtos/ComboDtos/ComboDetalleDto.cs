using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.ComboDtos
{
    public class ComboDetalleDto
    {
        public required int ProductoId { get; set; }
        public required int Cantidad { get; set; }
        public bool Opcional { get; set; } = false;
    }
}
