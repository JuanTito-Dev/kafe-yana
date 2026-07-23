using System.Text.Json.Serialization;

namespace KafeYana.Application.Dtos.VentaDtos
{
    /// <summary>
    /// Item de un cobro parcial (sub-venta): producto del catálogo + cantidad a
    /// cobrar. El backend decide de qué ronda(s) descontar (FIFO); el llamador
    /// nunca elige una fila de Detalle_ronda específica.
    /// </summary>
    public class DtoItemProductoCobrar
    {
        [JsonPropertyName("producto_id")]
        public int Id_Producto { get; set; }

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; }
    }
}
