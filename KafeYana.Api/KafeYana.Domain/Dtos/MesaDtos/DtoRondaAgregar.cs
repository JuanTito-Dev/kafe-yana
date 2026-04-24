using KafeYana.Domain.Dtos.RondaDtos;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.Dtos.MesaDtos
{
    public class DtoRondaAgregar
    {
        [Required(ErrorMessage = "El Id del pedido es obligatorio.")]
        public int Id_Pedido { get; set; }

        public List<DtoRondadetalle> Detalles {  get; set; }
    }
}
