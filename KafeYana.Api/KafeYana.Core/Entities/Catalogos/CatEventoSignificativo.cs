namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo paramétrico de eventos significativos del SIAT,
    /// sincronizado vía <c>sincronizarParametricaEventosSignificativos</c>.
    ///
    /// Lista oficial vigente (jun-2026, devuelta por el SIN):
    ///   1 = CORTE DEL SERVICIO DE INTERNET
    ///   2 = INACCESIBILIDAD AL SERVICIO WEB DE LA ADMINISTRACIÓN TRIBUTARIA
    ///   3 = INGRESO A ZONAS SIN INTERNET POR DESPLIEGUE DE PUNTO DE VENTA
    ///   4 = VENTA EN LUGARES SIN INTERNET
    ///   5 = VIRUS INFORMÁTICO O FALLA DE SOFTWARE
    ///   6 = CAMBIO DE INFRAESTRUCTURA DE SISTEMA O FALLA DE HARDWARE
    ///   7 = CORTE DE SUMINISTRO DE ENERGIA ELÉCTRICA
    ///
    /// Se refresca diariamente a las 08:10 BOT por
    /// <c>SincronizacionEventoSignificativoHostedService</c>.
    ///
    /// Fuera de scope: el uso real de este catálogo para facturación en
    /// contingencia (registro del evento, buffer offline, paquete) se
    /// implementa en un ticket separado.
    /// </summary>
    public class CatEventoSignificativo
    {
        public int Id { get; set; }

        /// <summary>Código numérico del evento (1..7 según catálogo SIN vigente).</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial devuelta por el SIN.</summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Marca de cuándo fue sincronizado por última vez.</summary>
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}
