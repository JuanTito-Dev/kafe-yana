namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo paramétrico de tipos de emisión del SIAT, sincronizado vía
    /// <c>sincronizarParametricaTipoEmision</c>.
    ///
    /// Catálogo UNIVERSAL: aplica a todos los contribuyentes, no se filtra por
    /// actividad económica.
    ///
    /// Lista oficial vigente (jun-2026, devuelta por el SIN):
    ///   1 = EN LINEA
    ///   2 = FUERA DE LINEA
    ///   3 = MASIVO
    ///   4 = CONTINGENCIA
    ///
    /// Hoy KafeYana sólo usa el código 1 ("EN LINEA") vía
    /// <c>SiatOptions.CodigoEmision</c> (default = 1). Este sync deja la
    /// infraestructura lista para cuando se implemente el flujo de contingencia
    /// (códigos 2 y 4) sin tener que volver a pegarle al SIAT en cada cambio.
    ///
    /// Se valida el valor hardcoded contra el caché en memoria
    /// <c>TipoEmisionSiatCatalogo</c> para detectar despublicaciones del SIN
    /// en el boot. El catálogo se refresca diariamente a las 08:10 BOT por
    /// <c>SincronizacionTipoEmisionHostedService</c>.
    /// </summary>
    public class CatTipoEmision
    {
        public int Id { get; set; }

        /// <summary>Código numérico del tipo de emisión (1..N según catálogo SIN vigente).</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial devuelta por el SIN (ej. "EN LINEA", "CONTINGENCIA").</summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Marca de cuándo fue sincronizado por última vez.</summary>
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}
