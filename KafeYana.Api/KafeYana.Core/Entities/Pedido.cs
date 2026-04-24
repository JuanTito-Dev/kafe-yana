using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Domain.Entities
{
    public class Pedido : BaseEntity
    {
        public int? Id_Cliente { get; set; }

        public decimal Total { get; set; } = 0.00M;

        public Mesa? Mesa { get; set; }

        public Cliente? Cliente { get; set; }

        public List<Ronda> Rondas { get; set; } = new List<Ronda>();

    }
}
