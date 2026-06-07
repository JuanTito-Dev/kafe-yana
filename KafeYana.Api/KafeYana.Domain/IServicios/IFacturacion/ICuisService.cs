using KafeYana.Domain.Entities.Facturacion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IServicios.IFacturacion
{
    public interface ICuisService
    {
        Task<Cuis> ObtenerCuisAsync(int codigoSucursal, int codigoPuntoVenta, CancellationToken ct = default);
        Task<Cuis> ObtenerCuisVigenteAsync(int codigoSucursal, int codigoPuntoVenta, CancellationToken ct = default);
    }
}
