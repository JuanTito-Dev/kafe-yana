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

            var utc = fecha.Kind switch

            {

                DateTimeKind.Utc => fecha,

                DateTimeKind.Local => fecha.ToUniversalTime(),

                _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc)

            };



            return TimeZoneInfo.ConvertTimeFromUtc(utc, ZonaBolivia);

        }

    }

}


