using KafeYana.Domain.TiposDeDatos;
using System.ComponentModel.DataAnnotations;

namespace KafeYana.Application.Dtos.PromocionPermanenteDtos
{
    public class DtoPromocionPermanenteCU
    {
        [Required]
        [MaxLength(100)]
        public required string Nombre { get; set; }

        [MaxLength(300)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [AllowedValues(
            TipoCondicionPromocion.NCompras,
            TipoCondicionPromocion.MontoMinimo,
            TipoCondicionPromocion.Requeridos,
            ErrorMessage = "TipoCondicion debe ser: NCompras, MontoMinimo o Requeridos")]
        public required string TipoCondicion { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ValorCondicion debe ser mayor a 0")]
        public int ValorCondicion { get; set; }

        [Required]
        [AllowedValues(
            TipoRecompensaPromocion.PuntosExtra,
            TipoRecompensaPromocion.ProductoGratis,
            TipoRecompensaPromocion.Descuento,
            ErrorMessage = "TipoRecompensa debe ser: PuntosExtra, ProductoGratis o Descuento")]
        public required string TipoRecompensa { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "ValorRecompensa no puede ser negativo")]
        public int ValorRecompensa { get; set; }

        [Required]
        public bool Activo { get; set; }

        /// <summary>Obligatorio solo cuando TipoRecompensa = ProductoGratis.</summary>
        public int? Id_ProductoCanjeable { get; set; }
    }
}
