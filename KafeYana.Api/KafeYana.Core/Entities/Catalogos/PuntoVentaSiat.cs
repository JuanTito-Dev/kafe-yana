namespace KafeYana.Domain.Entities.Catalogos
{
    /// <summary>
    /// Configuración de un punto de venta del sistema ante el SIAT.
    /// Permite manejar N sucursales/puntos de venta con una sola instalación del software.
    ///
    /// El CUIS/CUFD NO se guarda aquí porque ya existen las tablas Cuis y Cufd
    /// con vigencia propia. Se consultan dinámicamente por (sucursal, puntoVenta).
    /// </summary>
    public class PuntoVentaSiat
    {
        public int Id { get; set; }

        /// <summary>Código de sucursal asignado por el SIN (0 = Casa Matriz).</summary>
        public int CodigoSucursal { get; set; }

        /// <summary>Código de punto de venta asignado por el SIN (0 = único, 1+ = múltiples).</summary>
        public int CodigoPuntoVenta { get; set; }

        /// <summary>Nombre descriptivo para mostrar en UI / logs.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Si está activo, se incluye en la sincronización periódica.</summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Última vez que se ejecutó la sincronización de actividades del SIAT
        /// usando este (sucursal, puntoVenta). Sirve para auditoría.
        /// </summary>
        public DateTime? UltimaSyncActividades { get; set; }
    }
}