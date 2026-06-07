using KafeYana.Domain.Entities.Facturacion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface ICufdService
    {
        Task<Cufd> SolicitarCufdAsync(int codigoSucursal, int codigoPuntoVenta, CancellationToken ct = default);
        Task<Cufd> ObtenerCufdVigenteAsync(int codigoSucursal, int codigoPuntoVenta, CancellationToken ct = default);
    }
}
