using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.VentaDtos
{
    public class DtoVentaPedido
    {
        [Required]
        public required int Id_Pedido { get; set; }
        [Required]  
        public required int Id_Cliente { get; set; }
        
        [Required]
        public required TipoPagos TipoPago { get; set; }

        public decimal EfecctivoRecibido { get; set; } = 0.00M;
    }
}
