namespace KafeYana.Infrastructure.Servicios.SiatConnectivity
{
    /// <summary>
    /// Identificadores canónicos de las operaciones SOAP que ejecuta <c>SiatHttpClient</c>.
    /// Evita magic strings en la instrumentación del monitor de conectividad y permite
    /// que el monitor distinga operaciones críticas (CUIS/CUFD/FechaHora/RecepcionFactura)
    /// de las auxiliares (sync de catálogos), aplicando la regla de "solo las críticas
    /// cuentan para el umbral de detección de caída".
    ///
    /// Ver [[kafeyana-contingencia-siat]] — arquitectura de contingencia.
    /// </summary>
    public static class SiatOperacion
    {
        /// <summary>Solicitud de CUIS al SIN (operación <c>cuis</c>).</summary>
        public const string Cuis = "Cuis";

        /// <summary>Solicitud de CUFD vigente al SIN (operación <c>cufd</c>).</summary>
        public const string Cufd = "Cufd";

        /// <summary>Sincronización de fecha/hora oficial (operación <c>sincronizarFechaHora</c>).</summary>
        public const string FechaHora = "FechaHora";

        /// <summary>Recepción de factura (operación <c>recepcionFactura</c>).</summary>
        public const string RecepcionFactura = "RecepcionFactura";

        /// <summary>
        /// Recepción de paquete de N facturas contingencia (operación <c>recepcionPaqueteFactura</c>).
        /// Es el camino oficial para asociar facturas a un evento significativo vía SOAP
        /// (los campos <c>cantidadFacturas</c> + <c>codigoEvento</c> sólo existen en esta
        /// operación masiva del WSDL del ServicioFacturacion).
        /// </summary>
        public const string RecepcionPaqueteFactura = "RecepcionPaqueteFactura";

        /// <summary>
        /// FIX #1 — Validación asíncrona del paquete enviado (operación
        /// <c>validacionRecepcionPaqueteFactura</c>). Tras enviar un paquete contingencia
        /// con transaccion=true el SIN procesa en background; esta operación consulta el
        /// estado definitivo (901 pendiente / 904 observada / 908 validada). Marcada como
        /// crítica para que sus fallos cuenten en el umbral de detección.
        /// Ver [[kafeyana-contingencia-siat]] y documentacion-contingencia.md líneas 26-28.
        /// </summary>
        public const string ValidacionRecepcionPaqueteFactura = "ValidacionRecepcionPaqueteFactura";

        /// <summary>
        /// Registro de evento significativo (operación <c>registroEventoSignificativo</c>).
        /// El monitor NO cuenta sus fallos para evitar re-entrada (loop).
        /// </summary>
        public const string RegistroEventoSignificativo = "RegistroEventoSignificativo";

        /// <summary>Cierre de evento significativo (operación local, no SOAP).</summary>
        public const string CierreEvento = "CierreEvento";

        /// <summary>
        /// Operaciones auxiliares (sync de catálogos, anulaciones, notas de ajuste, etc.).
        /// Sus fallos NO se cuentan para el umbral porque no son del path crítico de cobro.
        /// </summary>
        public const string Otros = "Otros";
    }
}