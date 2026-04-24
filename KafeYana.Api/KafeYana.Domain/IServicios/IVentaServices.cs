using KafeYana.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios
{
    public interface IVentaServices
    {
        Task<Venta> ProcesarVenta(int Id_Pedido, int? Id_Cliente, string cajero, string tipoPago);
    }
}
