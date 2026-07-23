using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios.SiatConnectivity
{
    /// <summary>
    /// Monitor singleton del estado de conectividad con el SIAT.
    /// Es el "circuit breaker" liviano del módulo de contingencia: observa las
    /// llamadas SOAP que <c>SiatHttpClient</c> ejecuta y, cuando una cantidad
    /// configurable de operaciones críticas (CUIS/CUFD/FechaHora/RecepcionFactura)
    /// fallan consecutivamente dentro de una ventana móvil, dispara el
    /// registro automático de un evento significativo con motivo "CORTE DE INTERNET"
    /// (configurable via <see cref="DetectorOptions"/>).
    ///
    /// El monitor es <b>singleton</b> porque su estado (contadores de fallos,
    /// timestamps, mapa de contingencias activas por PV) debe sobrevivir entre
    /// requests HTTP. Para resolver servicios Scoped (como
    /// <c>IEventoSignificativoSiatService</c>) usa <c>IServiceScopeFactory</c>.
    ///
    /// Ver [[kafeyana-contingencia-siat]].
    /// </summary>
    public interface ISiatConnectivityMonitor
    {
        /// <summary>
        /// Carga al boot las contingencias activas que ya existen en BD,
        /// poblando el diccionario interno para que el monitor arrastre el
        /// estado correcto sin depender de que el próximo cobro lo detecte.
        /// </summary>
        Task InicializarAsync(CancellationToken ct = default);

        /// <summary>
        /// Notifica que la operación SOAP <paramref name="operacion"/> salió
        /// bien para el PV (<paramref name="codigoSucursal"/>, <paramref name="codigoPuntoVenta"/>).
        /// Si había un contador de fallos activo, lo resetea. Si había una
        /// contingencia activa registrada, dispara <see cref="OnRecuperacionDetectada"/>.
        /// </summary>
        Task ReportarExitoAsync(
            string operacion,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default);

        /// <summary>
        /// Notifica que la operación SOAP <paramref name="operacion"/> falló
        /// (timeout, 5xx, red, etc.) para el PV indicado. Si la operación es
        /// crítica y se acumulan <c>UmbralFallosConsecutivos</c> en menos de
        /// <c>VentanaFallosSegundos</c>, registra automáticamente la contingencia.
        /// </summary>
        Task ReportarFalloAsync(
            string operacion,
            Exception ex,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default);

        /// <summary>
        /// Devuelve el ID del evento significativo activo para el PV, o null
        /// si no hay contingencia. Útil para que otros servicios consulten el
        /// estado sin pasar por la BD.
        /// </summary>
        int? ObtenerEventoActivo(int codigoSucursal, int codigoPuntoVenta);

        /// <summary>
        /// Cortocircuito de "estoy online con el SIN" para que los consumidores
        /// (<c>SiatHttpClient</c> y demás) puedan evitar pegarle a la red cuando
        /// ya hay una contingencia activa registrada. Es el inverso de
        /// <see cref="ObtenerEventoActivo"/>: <c>true</c> cuando NO hay evento
        /// activo, <c>false</c> cuando lo hay (modo contingencia).
        /// </summary>
        bool EstaEnLinea(int codigoSucursal, int codigoPuntoVenta);

        /// <summary>
        /// Limpia el estado interno de contingencia para el PV. Lo llama el
        /// monitor cuando detecta recuperación, después de cerrar el evento.
        /// </summary>
        void LimpiarEstadoContingencia(int codigoSucursal, int codigoPuntoVenta);

        /// <summary>
        /// Registra en memoria una contingencia creada externamente (endpoint manual)
        /// sin pasar por el detector automático. Necesario para que el probe incluya
        /// este par en su ciclo y dispare el reenvío automático cuando el SIAT vuelva,
        /// sin requerir reinicio del backend.
        /// </summary>
        void NotificarContingenciaExterna(int codigoSucursal, int codigoPuntoVenta, int eventoId);

        /// <summary>
        /// Devuelve los pares (sucursal, puntoVenta) que el monitor está
        /// trackeando actualmente — típicamente los que tienen contingencia
        /// activa. Lo usa <c>SiatConnectivityProbeService</c> para iterar
        /// sólo sobre los pares que necesitan recovery detection, evitando
        /// generar tráfico innecesario cuando no hay contingencias abiertas.
        /// Devuelve una snapshot inmutable: un pair que se cierre/abra
        /// durante la iteración del probe no se verá afectado.
        /// </summary>
        IReadOnlyList<(int Sucursal, int PuntoVenta)> ObtenerParesMonitoreados();

        /// <summary>
        /// Publica manualmente un evento de recuperación para el PV. Lo usa
        /// <c>ContingencyBootstrapHostedService</c> al arranque para encolar
        /// el reenvío de ventas pendientes de contingencias que ya estaban
        /// activas en BD antes de que el monitor empezara a observar el SIAT.
        /// NO toca la BD ni cierra el evento: solo dispara el suscriptor.
        /// </summary>
        void PublicarRecuperacion(int codigoSucursal, int codigoPuntoVenta, int eventoId);

        /// <summary>
        /// Activa un bypass global del cortocircuito para el PV. Lo llama el
        /// probe cuando detecta que el SIAT está alcanzable. Una vez activo,
        /// todas las llamadas SOAP a este par pasan por encima del cortocircuito
        /// sin necesidad de propagar flags por la cadena de llamadas. Se
        /// desactiva automáticamente cuando la contingencia se cierra o cuando
        /// se llama <see cref="LimpiarEstadoContingencia"/>.
        ///
        /// Esto evita que el <c>stoppingToken</c> del probe se propague por
        /// toda la cadena de llamadas SOAP, lo cual causaba cancelaciones
        /// cruzadas en operaciones largas de BD.
        /// </summary>
        void ActivarBypass(int codigoSucursal, int codigoPuntoVenta);

        /// <summary>
        /// Limpia el guard <c>Pieza3bFalloUtc</c> del estado del monitor para
        /// el par. Lo llama el probe después de detectar SIAT vivo, dando
        /// UNA oportunidad de reintento para Pieza 3b en el mismo ciclo.
        ///
        /// <para>
        /// Es seguro limpiarlo porque el guard de re-entrada
        /// (<c>Pieza3bEnProgreso</c>) previene el bucle recursivo anterior:
        /// si el reintento falla de nuevo, sólo se setea
        /// <c>Pieza3bFalloUtc</c> otra vez hasta el próximo ciclo del probe
        /// (60s después). Sin esto, una vez que <c>Pieza3bFalloUtc</c> se
        /// setea por cualquier motivo (servicio mal mapeado, timeout de BD,
        /// error de configuración), el sistema queda bloqueado y requiere
        /// restart manual del backend para reintentar.
        /// </para>
        /// </summary>
        void LimpiarPieza3bFallo(int codigoSucursal, int codigoPuntoVenta);

        /// <summary>
        /// Devuelve <c>true</c> si el bypass del cortocircuito está activo
        /// para el par. Lo consulta <c>SiatHttpClient.EnviarSoapAsync</c> antes
        /// de chequear el cortocircuito normal.
        /// </summary>
        bool TieneBypassActivo(int codigoSucursal, int codigoPuntoVenta);

        /// <summary>
        /// Se dispara cuando una operación crítica vuelve a tener éxito después
        /// de una contingencia activa. Argumentos: (suc, pv, eventoId).
        /// El <c>ContingencyResendHostedService</c> se suscribe aquí para
        /// encolar el reenvío de las ventas pendientes.
        /// </summary>
        event Action<int, int, int>? OnRecuperacionDetectada;
    }
}