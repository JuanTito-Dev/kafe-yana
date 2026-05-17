using KafeYana.Domain.Entities;

namespace KafeYana.Application.IServicios
{
    public interface IPuntosService
    {
        /// <summary>
        /// Calcula los puntos que gana el cliente, los aplica sobre la entidad Cliente
        /// y registra el historial. Devuelve los puntos finales ganados (0 si no aplica).
        /// </summary>
        Task<int> CalcularYAplicarPuntosAsync(
            Cliente cliente,
            decimal totalVenta,
            bool tieneCombo,
            string codigoVenta);
    }
}
