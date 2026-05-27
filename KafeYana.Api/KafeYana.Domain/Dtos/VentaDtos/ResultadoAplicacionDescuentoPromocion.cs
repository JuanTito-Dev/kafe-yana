namespace KafeYana.Application.Dtos.VentaDtos

{

    public sealed class ResultadoAplicacionDescuentoPromocion

    {

        public int IdPromocion { get; init; }



        public required string NombrePromocion { get; init; }



        public int PorcentajeDescuento { get; init; }



        public decimal MontoDescuento { get; init; }



        public decimal SubtotalPedido { get; init; }



        public decimal TotalConDescuento { get; init; }



        public required string Mensaje { get; init; }

    }

}


