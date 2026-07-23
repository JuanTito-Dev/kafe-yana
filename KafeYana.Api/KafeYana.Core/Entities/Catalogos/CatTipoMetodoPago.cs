namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo paramétrico de tipos de método de pago del SIAT, sincronizado vía
    /// <c>sincronizarParametricaTipoMetodoPago</c>.
    ///
    /// Catálogo UNIVERSAL: aplica a todos los contribuyentes, no se filtra por
    /// actividad económica.
    ///
    /// El SIN publica ~308 códigos que se dividen en:
    ///   1..9   → métodos simples (EFECTIVO, TARJETA, CHEQUE, OTROS, etc.)
    ///   10..308 → combinaciones de 2 a 4 métodos (pagos mixtos).
    ///
    /// A diferencia de los otros catálogos sincronizados, este tiene un flag
    /// <see cref="Activo"/> controlado por el OPERADOR (no por el SIN): después
    /// del sync inicial, los códigos válidos llegan con <c>Activo=false</c>
    /// salvo los del seed default {1=EFECTIVO, 7=TRANSFERENCIA BANCARIA}. El
    /// operador activa los métodos adicionales desde un panel admin (out of
    /// scope de este PR).
    ///
    /// El sync es idempotente y conservador: <c>Activo</c> NUNCA se desactiva
    /// cuando el SIN ya no devuelve un código que el operador tenía habilitado
    /// (preserva la configuración manual).
    /// </summary>
    public class CatTipoMetodoPago
    {
        public int Id { get; set; }

        /// <summary>Código numérico del método de pago (1..308 según catálogo SIN vigente).</summary>
        public int Codigo { get; set; }

        /// <summary>Descripción oficial devuelta por el SIN (ej. "EFECTIVO", "TRANSFERENCIA BANCARIA").</summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Flag del operador: indica si el método está habilitado en el sistema.
        /// El sync del SIN NUNCA toca este campo (lo respeta).
        /// </summary>
        public bool Activo { get; set; }

        /// <summary>Marca de cuándo fue sincronizado por última vez contra el SIN.</summary>
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
    }
}