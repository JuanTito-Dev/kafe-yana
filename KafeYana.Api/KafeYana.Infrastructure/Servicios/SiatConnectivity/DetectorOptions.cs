namespace KafeYana.Infrastructure.Servicios.SiatConnectivity
{
    /// <summary>
    /// Configuración del detector automático de caída del SIAT.
    /// Mapeado desde appsettings.json sección "DetectorSiat".
    ///
    /// El monitor cruza el umbral cuando una operación crítica (CUIS/CUFD/FechaHora/RecepcionFactura)
    /// falla <see cref="UmbralFallosConsecutivos"/> veces seguidas dentro de
    /// <see cref="VentanaFallosSegundos"/> segundos. Al cruzar el umbral, se auto-registra
    /// un evento significativo con motivo <see cref="MotivoDefault"/> y origen "Automatico".
    ///
    /// Ver [[kafeyana-contingencia-siat]].
    /// </summary>
    public class DetectorOptions
    {
        public const string SeccionNombre = "DetectorSiat";

        /// <summary>
        /// Cantidad de fallos consecutivos de operaciones críticas dentro de la ventana
        /// para declarar el SIAT caído y registrar contingencia automática.
        /// Default: 2 (suficiente para distinguir "una llamada mala" de "caída real").
        /// </summary>
        public int UmbralFallosConsecutivos { get; set; } = 2;

        /// <summary>
        /// Ventana móvil en segundos dentro de la cual se acumulan los fallos consecutivos.
        /// Si entre el primer fallo y el último pasa más tiempo que esto, el contador se resetea.
        /// Default: 60s.
        /// </summary>
        public int VentanaFallosSegundos { get; set; } = 60;

        /// <summary>
        /// Tiempo mínimo en segundos entre el cierre de una contingencia y la apertura de la
        /// siguiente para evitar flapping. Si el SIAT oscila caído/recuperado, no abrimos
        /// contingencia nueva hasta que pase este tiempo desde la última.
        /// Default: 120s.
        /// </summary>
        public int TiempoMinimoRecuperacionSegundos { get; set; } = 120;

        /// <summary>
        /// Motivo del catálogo <c>CatEventosSignificativos</c> que el monitor usa por defecto
        /// cuando dispara la contingencia automática. Default 1 = "CORTE DEL SERVICIO DE INTERNET".
        /// </summary>
        public int MotivoDefault { get; set; } = 1;

        /// <summary>
        /// Descripción que se persiste en la contingencia cuando la dispara el monitor.
        /// Default: literal del catálogo para motivo=1.
        /// </summary>
        public string DescripcionDefault { get; set; } = "CORTE DEL SERVICIO DE INTERNET";
    }
}