namespace KafeYana.Application.Dtos.VentaDtos

{

    public sealed class DtoDescuentoDisponible

    {

        public int IdPromocion { get; init; }



        public required string Nombre { get; init; }



        public required string TipoCondicion { get; init; }



        public int ValorCondicion { get; init; }



        public int PorcentajeDescuento { get; init; }



        public decimal MontoDescuento { get; init; }



        public decimal TotalConDescuento { get; init; }

    }



    public sealed class DtoDescuentosPedidoRespuesta

    {

        public int Id_Pedido { get; init; }



        public int Id_Cliente { get; init; }



        public decimal SubtotalPedido { get; init; }



        public bool HayDescuentoDisponible { get; init; }



        public IReadOnlyList<DtoDescuentoDisponible> DescuentosDisponibles { get; init; } = [];



        public DtoDescuentoDisponible? DescuentoRecomendado { get; init; }

    }

}


