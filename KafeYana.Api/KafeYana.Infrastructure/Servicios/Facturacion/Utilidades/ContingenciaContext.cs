using System.Threading;

namespace KafeYana.Infrastructure.Servicios.Facturacion.Utilidades
{
    /// <summary>
    /// Contexto ambient (vía <see cref="AsyncLocal{T}"/>) para que múltiples
    /// servicios del flujo de contingencia compartan el correlation ID de un
    /// mismo intento de paquete sin pasarlo por todas las firmas.
    ///
    /// Setear <see cref="CorrelationId"/> desde el servicio que orquesta el
    /// intento (típicamente <c>RecepcionFacturaService.EnviarRecepcionPaqueteContingenciaAsync</c>
    /// al inicio). <c>SiatHttpClient.EnviarSoapAsync</c> lo lee y lo incluye
    /// en cada línea del log de archivo sin tener que recibirlo por parámetro.
    ///
    /// Se reusa el flujo async cuando una sola llamada pasa por varias
    /// invocaciones asíncronas; <see cref="AsyncLocal{T}"/> garantiza que la
    /// propagación por Task no rompa la asociación.
    /// Ver [[kafeyana-contingencia-siat]].
    /// </summary>
    public static class ContingenciaContext
    {
        private static readonly AsyncLocal<string?> _correlationId = new();

        /// <summary>
        /// Correlation ID actual (formato hex sin guiones, ej. <c>4762918350a3b1c2</c>).
        /// Devuelve <c>"(none)"</c> cuando no hay un intento de contingencia activo.
        /// </summary>
        public static string CorrelationId
        {
            get => _correlationId.Value ?? "(none)";
            set => _correlationId.Value = value;
        }
    }
}
