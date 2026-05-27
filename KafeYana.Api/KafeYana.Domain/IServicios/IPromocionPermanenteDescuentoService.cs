using KafeYana.Application.Dtos.VentaDtos;

using KafeYana.Domain.Entities;



namespace KafeYana.Application.IServicios

{

    public interface IPromocionPermanenteDescuentoService

    {

        /// <summary>Preview sin persistir progreso NCompras.</summary>

        Task<DtoDescuentosPedidoRespuesta> ObtenerDescuentosPedidoAsync(int idPedido, int idCliente);



        /// <summary>Evalúa, persiste historial y progreso. Null si ninguna promo califica.</summary>

        Task<ResultadoAplicacionDescuentoPromocion?> AplicarDescuentoAsync(

            Cliente cliente,

            decimal subtotalPedido,

            string codigoVenta);

    }

}


