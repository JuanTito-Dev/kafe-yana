namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Matriz Actividad ↔ Documento Sector sincronizada desde el SIAT
    /// vía <c>sincronizarListaActividadesDocumentoSector</c>.
    ///
    /// Define qué <see cref="CodigoDocumentoSector"/> puede emitir cada
    /// <see cref="CodigoActividad"/> (CAEB), junto con su
    /// <see cref="TipoDocumentoSector"/> oficial del SIN
    /// (FCV, NCD, NCDDE, FAC_CVB, …).
    ///
    /// PK lógica compuesta por <c>(CodigoActividad, CodigoDocumentoSector)</c>:
    /// el SIN nunca devuelve dos filas con la misma combinación, pero si las
    /// hubiera, las descartamos al insertar (índice único).
    /// </summary>
    public class CatActividadDocumentoSector
    {
        public int Id { get; set; }

        /// <summary>Código CAEB (ej. "4630600").</summary>
        public string CodigoActividad { get; set; } = string.Empty;

        /// <summary>Código de documento sector (ej. 1 = FCV, 24 = NCD, 35 = FAC_CVB, 47 = NCDDE).</summary>
        public int CodigoDocumentoSector { get; set; }

        /// <summary>Tipo de documento sector devuelto por el SIN (FCV, NCD, NCDDE, FAC_CVB, …).</summary>
        public string TipoDocumentoSector { get; set; } = string.Empty;

        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}
