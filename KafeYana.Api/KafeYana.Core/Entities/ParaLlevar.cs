using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class ParaLlevar : BaseEntity
    {
        public bool Disponible { get; set; } = true;
        public int? Id_Pedido { get; set; }
        public Pedido? Pedido { get; set; }
    }
}
