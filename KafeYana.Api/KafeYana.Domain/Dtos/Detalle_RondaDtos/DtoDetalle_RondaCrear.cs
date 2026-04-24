using System.Collections.Generic;

namespace KafeYana.Domain.Dtos.Detalle_RondaDtos
{
    public class DtoDetalle_RondaCrear
    {
        public int Id_Ronda { get; set; }

        public int Id_Producto { get; set; }

        public required string Nombre_Producto { get; set; }

        public int Cantidad { get; set; }

        public decimal Precio { get; set; } = 0.00M;

        public List<DtoDetalle_RondaOpcionCrear>? Opciones { get; set; }
    }
}