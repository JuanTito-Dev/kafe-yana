namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo paramétrico de motivos de anulación (factura y nota de crédito/débito)
    /// sincronizado desde el SIAT vía `sincronizarParametricaMotivoAnulacion`.
    ///
    /// El SIAT devuelve el mismo catálogo para ambas operaciones (anulación de factura
    /// y anulación de nota), por lo que KafeYana usa esta tabla única como fuente
    /// de verdad para validar `codigoMotivo` en ambos flujos.
    ///
    /// La lista oficial actual es:
    ///   1 = FACTURA MAL EMITIDA
    ///   2 = NOTA DE CREDITO-DEBITO MAL EMITIDA
    ///   3 = DATOS DE EMISION INCORRECTOS
    ///   4 = FACTURA O NOTA DE CREDITO-DEBITO DEVUELTA
    ///
    /// Se refresca diariamente a las 08:10 BOT por
    /// `SincronizacionMotivoAnulacionHostedService`.
    /// </summary>
    public class CatMotivoAnulacion
    {
        public int Id { get; set; }

        /// <summary>Código numérico del motivo (1..N según catálogo SIN vigente).</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial devuelta por el SIN.</summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Marca de cuándo fue sincronizado por última vez.</summary>
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}
