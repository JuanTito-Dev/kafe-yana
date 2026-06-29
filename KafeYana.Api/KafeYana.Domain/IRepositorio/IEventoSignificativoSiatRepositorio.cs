using System.Threading;
using System.Threading.Tasks;
using KafeYana.Domain.Entities.Facturacion;

namespace KafeYana.Application.IRepositorio
{
    /// <summary>
    /// Repositorio para <see cref="EventoSignificativoSiat"/>. Mantiene el log
    /// de eventos significativos registrados ante el SIN Bolivia y expone
    /// consultas orientadas al flujo de contingencia:
    ///   • <see cref="ObtenerContingenciaActivaAsync"/> — para que el wrapper
    ///     detector sepa si ya hay un evento abierto antes de registrar uno nuevo.
    ///   • <see cref="CerrarContingenciaAsync"/> — al recuperar el SIAT, se
    ///     marca el evento Activo como Cerrado con la fecha real de fin.
    /// </summary>
    public interface IEventoSignificativoSiatRepositorio : IGenericRepositorio<EventoSignificativoSiat>
    {
        /// <summary>
        /// Devuelve la contingencia activa más reciente (Estado='Activo' más nueva
        /// por FechaRegistro DESC). Si no hay ninguna, devuelve null.
        /// </summary>
        Task<EventoSignificativoSiat?> ObtenerContingenciaActivaAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default);

        /// <summary>
        /// Cierra la contingencia indicada: setea <c>EstadoContingencia='Cerrado'</c>
        /// y <c>FechaCierre=DateTime.UtcNow</c>. Si ya estaba cerrada, no hace nada.
        /// </summary>
        Task CerrarContingenciaAsync(int eventoSignificativoId, CancellationToken ct = default);

        /// <summary>
        /// Devuelve TODAS las contingencias activas (Estado='Activo') en la BD,
        /// independientemente de (suc, pv). Lo consume el bootstrap del monitor
        /// al arrancar el servidor para hidratar su diccionario interno.
        /// </summary>
        Task<List<EventoSignificativoSiat>> ListarContingenciasActivasAsync(
            CancellationToken ct = default);
    }
}