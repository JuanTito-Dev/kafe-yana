using System.Globalization;

using System.Runtime.InteropServices;



namespace KafeYana.Infrastructure.Servicios.Facturacion.Utilidades

{

    /// <summary>
    /// Fechas SIAT en hora de Bolivia (America/La_Paz, UTC-4).
    /// Formato XML/WS: "yyyy-MM-ddTHH:mm:ss.fff" sin sufijo de zona.
    /// </summary>

    public static class SiatFechaEmision

    {

        private const string FormatoXml = "yyyy-MM-dd'T'HH:mm:ss.fff";



        public static readonly TimeZoneInfo ZonaBolivia = TimeZoneInfo.FindSystemTimeZoneById(

            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)

                ? "SA Western Standard Time"

                : "America/La_Paz");



        public static DateTime AhoraUtc() =>

            DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);



        public static DateTime AhoraBolivia() =>

            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaBolivia);



        /// <summary>
        /// Convierte una fecha BOT (Unspecified) a DateTime con Kind=Utc sumando 4h,
        /// para persistir en columnas "timestamp with time zone" de PostgreSQL.
        /// Si la fecha ya viene Utc o Local, primero la normaliza a UTC.
        /// </summary>
        public static DateTime ToUtcForDb(DateTime fecha)
        {

            return fecha.Kind switch
            {

                DateTimeKind.Utc => fecha,

                DateTimeKind.Local => fecha.ToUniversalTime(),

                _ => TimeZoneInfo.ConvertTimeToUtc(
                        DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified), ZonaBolivia)

            };

        }



        public static string Formatear(DateTime fecha)

        {

            var bolivia = ConvertirABolivia(fecha);

            return bolivia.ToString(FormatoXml, CultureInfo.InvariantCulture);

        }



        public static string FormatearParaCuf(DateTime fecha)

        {

            var bolivia = ConvertirABolivia(fecha);

            return bolivia.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)

                + bolivia.Millisecond.ToString("D3", CultureInfo.InvariantCulture);

        }



        private static DateTime ConvertirABolivia(DateTime fecha)

        {

            // El SIAT devuelve hora BOT (marcada como Unspecified por SiatHttpClient.ParseFecha).
            // Como ya viene en hora BOT, la devolvemos tal cual para que el XML lleve exactamente
            // lo que devolvió el SIAT (sin doble conversión que provocaba el error 1009).
            // Si llega como UTC, se convierte a BOT; si llega como Local, primero a UTC.
            return fecha.Kind switch

            {

                DateTimeKind.Unspecified => fecha,

                DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(fecha, ZonaBolivia),

                DateTimeKind.Local => TimeZoneInfo.ConvertTimeFromUtc(fecha.ToUniversalTime(), ZonaBolivia),

                _ => fecha

            };

        }

    }

}