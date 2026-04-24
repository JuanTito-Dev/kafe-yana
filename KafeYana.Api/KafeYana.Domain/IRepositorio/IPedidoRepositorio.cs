using KafeYana.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IPedidoRepositorio : IGenericRepositorio<Pedido>
    {
        Task<Pedido?> TraerPedido(int Id);


    }
}
