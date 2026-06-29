namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo paramétrico de unidades de medida del SIAT, sincronizado vía
    /// <c>sincronizarParametricaUnidadMedida</c>.
    ///
    /// Catálogo UNIVERSAL: aplica a todos los contribuyentes, no se filtra por
    /// actividad económica. El SIN publica ~50–100 códigos (UNIDAD, VASO,
    /// BOTELLA, CAJA, KILOGRAMO, LITRO, MILILITRO, etc.) que se usan en el
    /// <c>&lt;unidadMedida&gt;</c> del detalle de la factura y la nota C/D.
    ///
    /// A diferencia de los otros catálogos sincronizados, este tiene un flag
    /// <see cref="Activo"/> controlado por el OPERADOR (no por el SIN):
    /// después del sync inicial, los códigos válidos llegan con
    /// <c>Activo=false</c> salvo los del seed default {los 9 códigos que la
    /// cafetería ya usa: UNIDAD=57, VASO=97, BOTELLA=5, CAJA=6, MILIGRAMO=33,
    /// GRAMO=17, LITRO=28, MILILITRO=34, OTRO=62}. El operador activa las
    /// unidades adicionales desde un panel admin (out of scope de este sync).
    ///
    /// El sync es idempotente y conservador: <c>Activo</c> NUNCA se desactiva
    /// cuando el SIN ya no devuelve un código que el operador tenía habilitado
    /// (preserva la configuración manual).
    /// </summary>
    public class CatUnidadMedida
    {
        public int Id { get; set; }

        /// <summary>Código numérico de la unidad de medida según catálogo SIN vigente.</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial devuelta por el SIN (ej. "UNIDAD", "VASO", "LITRO").</summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Flag del operador: indica si la unidad está habilitada en el sistema.
        /// El sync del SIN NUNCA toca este campo (lo respeta).
        /// </summary>
        public bool Activo { get; set; }

        /// <summary>Marca de cuándo fue sincronizado por última vez contra el SIN.</summary>
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}