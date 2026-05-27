using System.ComponentModel.DataAnnotations;



namespace KafeYana.Application.Dtos.VentaDtos

{

    public class DtoVentaPedido

    {

        [Required]

        public required int Id_Pedido { get; set; }



        [Required]

        public required int Id_Cliente { get; set; }



        [Required]

        public required DtoPagos Pagos { get; set; }



        /// <summary>Si es true, aplica el mejor descuento permanente disponible. Default: false.</summary>

        public bool AplicarDescuentos { get; set; } = false;

    }

}


