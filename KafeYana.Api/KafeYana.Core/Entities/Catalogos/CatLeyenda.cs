namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo paramétrico de leyendas obligatorias del SIAT, filtrado por la
    /// actividad económica principal (CAEB) del operador.
    ///
    /// Sincronizado desde el SIAT vía <c>sincronizarListaLeyendasFactura</c>
    /// por <c>SincronizadorCatLeyenda</c>. El SIAT devuelve la lista completa
    /// de leyendas para TODAS las actividades económicas del NIT; KafeYana
    /// filtra ANTES de persistir para mantener la tabla chica y específica del
    /// operador (solo las leyendas del CAEB principal).
    ///
    /// Cada actividad económica puede tener N leyendas (la SIAT publica
    /// "Ley N° 453" con varias cláusulas: reclamación, no discriminación,
    /// modalidades de entrega, etc.), así que la PK lógica es la combinación
    /// <c>(CodigoActividad, DescripcionLeyenda)</c>.
    ///
    /// Se refresca diariamente a las 08:10 BOT por
    /// <c>SincronizacionLeyendaHostedService</c>.
    /// </summary>
    public class CatLeyenda
    {
        public int Id { get; set; }

        /// <summary>Código CAEB al que aplica esta leyenda.</summary>
        public string CodigoActividad { get; set; } = string.Empty;

        /// <summary>Texto oficial devuelto por el SIN (ej. "Ley N° 453: ...").</summary>
        public string DescripcionLeyenda { get; set; } = string.Empty;

        /// <summary>Marca de cuándo fue sincronizado por última vez.</summary>
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}
