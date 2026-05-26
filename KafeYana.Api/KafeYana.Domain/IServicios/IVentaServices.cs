using KafeYana.Application.Dtos.VentaDtos;

namespace KafeYana.Application.IServicios
{
    public interface IVentaServices
    {
        Task<ResultadoProcesarVenta> ProcesarVenta(DtoVentaPedido datos, string cajero);
    }
}
