using KafeYana.Application.Dtos.ProductoCanjeable;

namespace KafeYana.Application.IServicios;

public interface IPromocionPermanenteProductoGratisService
{
    Task<DtoPromocionesGratisCliente> ObtenerPromocionesGratisClienteAsync(int idCliente);

    Task<ResultadoReclamoPromocionGratis> ReclamarAsync(DtoReclamarPromocionGratis dto);

    Task RegistrarProgresoPostVentaAsync(int idCliente, decimal subtotalVenta);
}
