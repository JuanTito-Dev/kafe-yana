namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo paramétrico de países de origen del SIAT,
    /// sincronizado vía <c>sincronizarParametricaPaisOrigen</c>.
    ///
    /// Devuelve ~211 países (lista oficial vigente jun-2026, devuelta por el SIN).
    /// Catálogo UNIVERSAL: no se filtra por actividad económica.
    ///
    /// Uso previsto (fuera de scope de este ticket):
    ///   - Factura Comercial de Exportación (Documento Sector 3): código de
    ///     país de destino de la mercadería, requerido por el SIN.
    ///   - Clientes extranjeros identificados con Pasaporte o Cédula de
    ///     Identidad de Extranjero (E): país de origen del documento.
    ///
    /// Se refresca diariamente a las 08:10 BOT por
    /// <c>SincronizacionPaisOrigenHostedService</c>.
    /// </summary>
    public class CatPaisOrigen
    {
        public int Id { get; set; }

        /// <summary>Código numérico del país (1..211 según catálogo SIN vigente).</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial devuelta por el SIN (ej. "BOLIVIA (ESTADO PLURINACIONAL DE)").</summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Marca de cuándo fue sincronizado por última vez.</summary>
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}