using System.Threading;
using System.Threading.Tasks;
using KafeYana.Application.Dtos.FacturacionDtos;

namespace KafeYana.Application.IServicios.IFacturacion
{
    /// <summary>
    /// Servicio de eventos significativos SIAT Bolivia. Orquesta:
    ///   • <see cref="RegistrarEventoAsync"/> — invoca <c>registroEventoSignificativo</c>
    ///     contra el SIN y persiste la respuesta (con <c>codigoRecepcionEventoSignificativo</c>)
    ///     en BD. Es invocable tanto por el cajero (origen="Manual") como por el
    ///     wrapper detector automático (origen="Automatico").
    ///   • <see cref="ObtenerEstadoContingenciaAsync"/> — devuelve el estado actual
    ///     de contingencia activa para un PV. Lo consume el frontend para decidir
    ///     qué mostrar, y <c>VentaServices</c> para saber si debe emitir en modo offline.
    ///   • <see cref="ListarHistorialAsync"/> — log paginado para auditoría y para
    ///     que el operador vea el historial de cortes pasados.
    ///   • <see cref="CerrarContingenciaAsync"/> — al recuperar el SIAT, cierra el
    ///     evento Activo para que las próximas facturas se emitan en línea.
    ///
    /// Regla clave: solo puede haber UNA contingencia Activa por (sucursal, PV).
    /// El método <see cref="RegistrarEventoAsync"/> rechaza si ya hay una abierta.
    ///
    /// Ver [[kafeyana-contingencia-siat]] — backend autoridad, frontend reacciona.
    /// </summary>
    public interface IEventoSignificativoSiatService
    {
        Task<ResultadoRegistroEventoSignificativoDto> RegistrarEventoAsync(
            DtoRegistrarEventoSignificativo dto,
            CancellationToken ct = default);

        /// <summary>
        /// Versión simplificada para el wrapper detector automático.
        /// Toma los defaults razonables y resuelve codigoSucursal/codigoPuntoVenta
        /// desde <paramref name="codigoSucursal"/>/<paramref name="codigoPuntoVenta"/>
        /// (obligatorios en este path). El caller delega la decisión de motivo,
        /// descripción y origen.
        /// </summary>
        Task<ResultadoRegistroEventoSignificativoDto> RegistrarYActivarAsync(
            int motivo,
            string origen,
            int codigoSucursal,
            int codigoPuntoVenta,
            string descripcion,
            CancellationToken ct = default);

        /// <summary>
        /// Registra una contingencia en BD LOCAL sin invocar al SIAT (sin sobre
        /// SOAP <c>registroEventoSignificativo</c>). Pensado como fallback del
        /// wrapper detector cuando el umbral de fallos se cruza pero el SIN está
        /// caído y por lo tanto <see cref="RegistrarYActivarAsync"/> falla.
        ///
        /// Persiste el evento con <c>CodigoRecepcionEventoSignificativo = null</c>,
        /// <c>Transaccion = false</c>, <c>Origen = "AutomaticoSinSoap"</c> y
        /// <c>EstadoContingencia = Activo</c>. El reenvío del evento al SIAT
        /// (obtención del <c>CodigoRecepcion</c> real) se hace en
        /// <see cref="ReenviarRegistroAsync"/> cuando el monitor detecta
        /// recuperación.
        ///
        /// NO requiere CUIS ni CUFD frescos (no arma sobre SOAP). El campo
        /// <c>CufdEvento</c> se rellena con lo que haya en BD vía
        /// <c>ICufdService.ObtenerCufdEnCacheAsync</c> (puede ser vacío si la
        /// tabla está vacía — modo contingencia degradado).
        /// </summary>
        Task<ResultadoRegistroEventoSignificativoDto> RegistrarLocalmenteSinSoapAsync(
            int motivo,
            string origen,
            int codigoSucursal,
            int codigoPuntoVenta,
            string descripcion,
            CancellationToken ct = default);

        /// <summary>
        /// Reenvía al SIAT el registro de un evento significativo que fue
        /// persistido localmente sin SOAP (origen "AutomaticoSinSoap",
        /// <c>CodigoRecepcionEventoSignificativo = null</c>).
        ///
        /// Si el evento ya tiene <c>CodigoRecepcion</c> poblado, retorna
        /// idempotente sin hacer nada. Si no, resuelve CUIS/CUFD frescos
        /// (asume que el monitor ya detectó recuperación), arma el sobre SOAP
        /// <c>registroEventoSignificativo</c>, y hace UPDATE del entity
        /// existente con el <c>CodigoRecepcion</c> real. NO crea un evento
        /// nuevo.
        ///
        /// Llamado desde el monitor en el camino de recuperación (Pieza 3).
        /// </summary>
        Task<ResultadoRegistroEventoSignificativoDto> ReenviarRegistroAsync(
            int eventoSignificativoId,
            CancellationToken ct = default);

        Task<EstadoContingenciaDto> ObtenerEstadoContingenciaAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default);

        Task<List<EventoSignificativoHistorialDto>> ListarHistorialAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            int limite,
            CancellationToken ct = default);

        Task CerrarContingenciaAsync(
            int eventoSignificativoId,
            CancellationToken ct = default);

        /// <summary>
        /// Devuelve TODAS las contingencias activas en la BD. Lo consume el
        /// <c>ISiatConnectivityMonitor.InicializarAsync</c> al boot para hidratar
        /// su diccionario interno sin esperar al próximo cobro.
        /// </summary>
        Task<List<ContingenciaActivaDto>> ListarContingenciasActivasAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Auto-expira un evento contingencia activo: cambia su estado a
        /// <c>EventoContingenciaEstado.AutoExpirado</c> porque su
        /// <c>FechaHoraInicio</c> excede el límite normativo del SIN
        /// (<see cref="Configuration.SiatOptions.HorasMaximaContingenciaAbierta"/>
        /// — default 48h). Marca también las ventas Pendientes asociadas con
        /// <c>ErrorMensaje</c> claro.
        ///
        /// Usado por el monitor en <c>InicializarAsync</c> para evitar SOAP calls
        /// inútiles a eventos que el SIN rechazará con error 981
        /// (rango de fechas inválido).
        ///
        /// No-op si el evento ya está en estado terminal (Cerrado, Rechazado,
        /// AutoExpirado) — el método es idempotente.
        ///
        /// Devuelve la cantidad de ventas Pendientes marcadas con error.
        /// </summary>
        Task<int> AutoExpirarEventoAsync(
            int eventoSignificativoId,
            CancellationToken ct = default);
    }
}