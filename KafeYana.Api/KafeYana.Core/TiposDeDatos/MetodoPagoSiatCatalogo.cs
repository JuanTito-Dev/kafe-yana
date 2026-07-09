using System.Collections.Concurrent;

namespace KafeYana.Domain.TiposDeDatos
{
    /// <summary>
    /// Caché estático en memoria del catálogo paramétrico de métodos de pago del
    /// SIAT (<c>sincronizarParametricaTipoMetodoPago</c>).
    ///
    /// Espejo de <see cref="TipoEmisionSiatCatalogo"/>, extendido con el flag
    /// <c>Activo</c> controlado por el operador (no por el SIN).
    ///
    /// El caché se refresca desde <c>SincronizadorCatMetodosPago</c> cuando
    /// corre el sync (al boot del server + manual vía
    /// <c>POST /api/catalogos/sincronizar-metodos-pago</c>). NO se sincroniza
    /// diario.
    ///
    /// Thread-safety: el snapshot se reemplaza atómicamente vía
    /// <see cref="Volatile.Write"/> + lectura con <see cref="Volatile.Read"/>.
    /// </summary>
    public static class MetodoPagoSiatCatalogo
    {
        /// <summary>
        /// Códigos de método de pago aceptados oficialmente por el SIN
        /// vigentes a jun-2026. Se usan como fallback antes del primer sync
        /// del server y para que el sistema siga funcionando si el sync
        /// posterior falla.
        ///
        /// Cobertura mínima:
        ///   1 = EFECTIVO
        ///   2 = TARJETA
        ///   5 = OTROS
        ///   7 = TRANSFERENCIA BANCARIA (alias QR en KafeYana)
        /// </summary>
        private static readonly Dictionary<int, string> FallbackHardcoded =
            new()
            {
                [1] = "EFECTIVO",
                [2] = "TARJETA",
                [5] = "OTROS",
                [7] = "TRANSFERENCIA BANCARIA",
            };

        /// <summary>Seed default: códigos que arrancan <c>Activo=true</c> en el primer sync.</summary>
        private static readonly HashSet<int> SeedActivosPorDefault = new() { 1, 2, 7 };

        /// <summary>
        /// Snapshot inmutable del catálogo (código → (descripción, activo)).
        /// Antes del primer sync se inicializa con el fallback hardcoded
        /// y todos los códigos marcados como activos para no romper el flujo
        /// legacy mientras corre el boot.
        /// </summary>
        private static volatile Dictionary<int, (string Descripcion, bool Activo)> _snapshot =
            FallbackHardcoded.ToDictionary(kv => kv.Key, kv => (kv.Value, true));

        /// <summary>
        /// True cuando el caché sigue siendo el fallback hardcoded (todavía
        /// no corrió ningún sync del SIAT en este proceso).
        /// </summary>
        public static bool EsFallback { get; private set; } = true;

        /// <summary>
        /// Limpia el snapshot al estado inicial (útil para tests).
        /// </summary>
        public static void ResetearAFallback()
        {
            Volatile.Write(
                ref _snapshot,
                FallbackHardcoded.ToDictionary(kv => kv.Key, kv => (kv.Value, true)));
            EsFallback = true;
        }

        /// <summary>
        /// Reemplaza el snapshot del catálogo con los datos del SIN.
        ///
        /// Reglas de merge:
        ///   - Códigos nuevos del SIN que no estaban → arrancan <c>Activo=true</c>
        ///     si pertenecen al seed default (1, 7), si no <c>Activo=false</c>.
        ///   - Códigos existentes → se actualiza solo <c>Descripcion</c>; el flag
        ///     <c>Activo</c> se PRESERVA (config del operador).
        ///   - Códigos que estaban en la BD pero ya no devuelve el SIN → se
        ///     MANTIENEN en el snapshot con su estado anterior (no se borran,
        ///     preserva auditoría).
        /// </summary>
        /// <param name="metodosSiat">Lista cruda del SIN (codigo, descripcion).</param>
        /// <param name="estatusExistente">
        /// Mapa código → <c>Activo</c> actual en BD (puede ser null si el
        /// caller no lo tiene; en ese caso se aplica el seed default).
        /// </param>
        public static void Refrescar(
            IEnumerable<(int Codigo, string Descripcion)> metodosSiat,
            IDictionary<int, bool>? estatusExistente = null)
        {
            var snapshotActual = Volatile.Read(ref _snapshot);
            var nuevo = new Dictionary<int, (string Descripcion, bool Activo)>(snapshotActual);

            foreach (var (codigo, descripcion) in metodosSiat)
            {
                if (codigo <= 0 || string.IsNullOrWhiteSpace(descripcion)) continue;

                bool activo;
                if (estatusExistente is not null && estatusExistente.TryGetValue(codigo, out var a))
                {
                    // Preservar config del operador.
                    activo = a;
                }
                else if (nuevo.TryGetValue(codigo, out var existente))
                {
                    // Estaba en el snapshot previo pero el caller no pasó
                    // estatusExistente → mantener su flag.
                    activo = existente.Activo;
                }
                else
                {
                    // Código nuevo del SIN → aplicar seed default.
                    activo = SeedActivosPorDefault.Contains(codigo);
                }

                nuevo[codigo] = (descripcion.Trim(), activo);
            }

            Volatile.Write(ref _snapshot, nuevo);
            EsFallback = false;
        }

        /// <summary>Devuelve un mapa completo (código → descripción).</summary>
        public static IReadOnlyDictionary<int, string> ObtenerTodos()
        {
            return Volatile.Read(ref _snapshot)
                .ToDictionary(kv => kv.Key, kv => kv.Value.Descripcion);
        }

        /// <summary>Devuelve solo los métodos con <c>Activo=true</c> (los que el operador habilitó).</summary>
        public static IReadOnlyDictionary<int, string> ObtenerActivos()
        {
            return Volatile.Read(ref _snapshot)
                .Where(kv => kv.Value.Activo)
                .ToDictionary(kv => kv.Key, kv => kv.Value.Descripcion);
        }

        /// <summary>True si el código existe en el catálogo vigente.</summary>
        public static bool EsValido(int codigo)
        {
            return Volatile.Read(ref _snapshot).ContainsKey(codigo);
        }

        /// <summary>True si el código está marcado como activo por el operador.</summary>
        public static bool EstaActivo(int codigo)
        {
            return Volatile.Read(ref _snapshot).TryGetValue(codigo, out var v) && v.Activo;
        }

        /// <summary>
        /// True si el código es válido Y está activo. Es la combinación que
        /// usan los DTOs para rechazar pagos contra métodos deshabilitados.
        /// </summary>
        public static bool EsValidoYActivo(int codigo)
        {
            return Volatile.Read(ref _snapshot).TryGetValue(codigo, out var v) && v.Activo;
        }

        /// <summary>Devuelve la descripción oficial del código, o null si no existe.</summary>
        public static string? ObtenerDescripcion(int codigo)
        {
            return Volatile.Read(ref _snapshot).TryGetValue(codigo, out var v) ? v.Descripcion : null;
        }
    }
}