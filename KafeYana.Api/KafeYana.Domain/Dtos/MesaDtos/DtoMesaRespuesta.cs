using KafeYana.Application.Dtos.PedidoDtos;
using KafeYana.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.MesaDtos
{
    public class DtoMesaRespuesta
    {
        public int Id { get; set; }
        public required string Nombre { get; set; }

        public int? Id_Pedido { get; set; }

        public bool Disponible { get; set; } = true;

        public DtoPedidoRespuesta pedido { get; set; }
    }
}
