namespace KafeYana.Infrastructure.Configuration
{
    /// <summary>
    /// Config del probe periódico de SIAT. Mapeado desde appsettings.json
    /// sección <c>"SiatProbe"</c>.
    ///
    /// El probe existe para romper el chicken-and-egg del monitor: cuando
    /// hay una contingencia Activa en memoria, todas las llamadas SOAP
    /// reales caen en el cortocircuito de <c>SiatHttpClient.cs:1532</c> y
    /// el monitor nunca ve tráfico real que le permita detectar recuperación.
    /// El probe hace un HTTP GET barato al <c>BaseAddress</c> sin pasar por
    /// el cortocircuito; si SIAT responde, llama
    /// <c>monitor.ReportarExitoAsync("Cuis", suc, pv, ct)</c> que dispara
    /// Pieza 3b (ReenviarRegistroAsync) + Pieza 4 (reenvío de facturas) +
    /// cierre de contingencia.
    /// </summary>
    public class SiatConnectivityProbeOptions
    {
        public const string SeccionNombre = "SiatProbe";

        /// <summary>
        /// Si está en false, el probe no corre. Útil para deshabilitarlo
        /// temporalmente sin recompilar.
        /// </summary>
        public bool Habilitado { get; set; } = true;

        /// <summary>
        /// Cada cuántos segundos el probe revisa si SIAT volvió.
        /// Default 60s — balance entre latencia de detección y tráfico al SIAT.
        /// </summary>
        public int IntervaloSegundos { get; set; } = 60;

        /// <summary>
        /// Timeout por par (suc, pv) en un ciclo. Si un ping tarda más,
        /// se cancela y se considera caído para esta vuelta.
        /// Default 10s — bastante menos que el TimeoutSegundos=30 de SiatOptions
        /// porque solo queremos saber "SIAT responde algo", no esperar SOAP.
        /// </summary>
        public int TimeoutSegundos { get; set; } = 10;

        /// <summary>
        /// Espera inicial al boot antes de empezar a probar. Le da tiempo
        /// al ContingencyBootstrapHostedService a hidratar el monitor.
        /// Default 15s.
        /// </summary>
        public int DemoraInicialSegundos { get; set; } = 15;
    }
}
