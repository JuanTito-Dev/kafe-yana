using KafeYana.Domain.Entities;



namespace KafeYana.Application.Dtos.VentaDtos

{

    public sealed class ResultadoProcesarVenta

    {

        public required Venta Venta { get; init; }



        public int PuntosPorVenta { get; init; }



        public ResultadoAplicacionPromocionPermanente? PromocionPermanente { get; init; }



        public ResultadoAplicacionDescuentoPromocion? DescuentoPromocion { get; init; }

    }

}


