namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo de actividades económicas (CAEB) sincronizado desde el SIAT.
    /// Se refresca periódicamente vía SiatSincronizacionService.
    /// </summary>
    public class CatActividad
    {
        public int Id { get; set; }

        /// <summary>Código CAEB que devuelve el SIN (ej. "5610200").</summary>
        public string CodigoCaeb { get; set; } = string.Empty;

        /// <summary>Descripción de la actividad (ej. "Servicio de comida en cafeterías").</summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Tipo de actividad según el SIN.</summary>
        public string TipoActividad { get; set; } = string.Empty;

        /// <summary>Marca de cuándo fue sincronizado por última vez.</summary>
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}