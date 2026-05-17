using KafeYana.Domain.TiposDeDatos;
using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.ProductoCanjeable
{
    public class DtoProductoCanjeableCU
    {
        [Required]
        public int Id_Producto { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Los puntos deben ser mayor a 0")]
        public int Puntos { get; set; }

        [Required]
        [AllowedValues(TipoDisponibilidad.Mesas, TipoDisponibilidad.ParaLlevar, TipoDisponibilidad.MesasYParaLlevar,
            ErrorMessage = "Disponible debe ser: Mesas, ParaLlevar o MesasYParaLlevar")]
        public required string Disponible { get; set; }

        [Required]
        public bool Activo { get; set; }
    }
}
