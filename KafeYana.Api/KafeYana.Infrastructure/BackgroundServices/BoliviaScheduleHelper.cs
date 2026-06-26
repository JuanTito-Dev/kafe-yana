namespace KafeYana.Infrastructure.BackgroundServices
{
    /// <summary>
    /// Helper compartido para los <see cref="Microsoft.Extensions.Hosting.BackgroundService"/>
    /// que corren sincronizaciones diarias a una hora fija en hora de Bolivia
    /// (UTC-4, sin DST).
    ///
    /// Bolivia no usa horario de verano, así que UTC-4 es constante todo el año.
    /// Si el server está en otra zona horaria, el cálculo compensa usando
    /// <see cref="TimeZoneInfo"/>. Se resuelve primero por el id de Windows
    /// (<c>SA Pacific Standard Time</c>) y luego por el id IANA de Linux/macOS
    /// (<c>America/La_Paz</c>), con fallback a <see cref="TimeZoneInfo.Utc"/>.
    ///
    /// Antes este código vivía duplicado en <c>SincronizacionMotivoAnulacionHostedService</c>;
    /// al agregarse los syncs de Actividades (migrado) y Leyendas (nuevo), se
    /// extrajo acá para evitar divergencia entre los 3 hosted services.
    /// </summary>
    public static class BoliviaScheduleHelper
    {
        /// <summary>
        /// Zona horaria Bolivia (UTC-4, sin DST). Inicialización lazy para no
        /// tocar <see cref="TimeZoneInfo"/> si nadie usa este helper.
        /// </summary>
        public static TimeZoneInfo BoliviaTz { get; } =
            TryFindTz("SA Pacific Standard Time")
            ?? TryFindTz("America/La_Paz")
            ?? TimeZoneInfo.Utc;

        /// <summary>
        /// Calcula el <see cref="TimeSpan"/> que falta hasta la próxima
        /// <paramref name="horaObjetivo"/> en hora BOT.
        /// Si justo ahora son las <paramref name="horaObjetivo"/>, devuelve 24h
        /// para evitar un doble tick.
        ///
        /// Si el server estuvo caído varias horas/días, al levantar siempre
        /// calcula el próximo slot futuro: NO se acumulan ticks perdidos.
        /// </summary>
        public static TimeSpan UntilNextRun(TimeSpan horaObjetivo)
        {
            var ahoraUtc = DateTime.UtcNow;
            var ahoraBot = TimeZoneInfo.ConvertTimeFromUtc(ahoraUtc, BoliviaTz);

            var hoyObjetivo = ahoraBot.Date + horaObjetivo;
            DateTime proximoObjetivo;
            if (ahoraBot < hoyObjetivo)
                proximoObjetivo = hoyObjetivo;
            else
                proximoObjetivo = hoyObjetivo.AddDays(1);

            var proximoObjetivoUtc = TimeZoneInfo.ConvertTimeToUtc(proximoObjetivo, BoliviaTz);
            return proximoObjetivoUtc - ahoraUtc;
        }

        private static TimeZoneInfo TryFindTz(string id)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { return null; }
        }
    }
}
