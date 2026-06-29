namespace KafeYana.Domain.TiposDeDatos
{
    public record UnidadMedidaSiatItem(int Codigo, string Descripcion);

    /// <summary>
    /// Caché estático en memoria del catálogo paramétrico de unidades de medida
    /// del SIAT (<c>sincronizarParametricaUnidadMedida</c>).
    ///
    /// Espejo de <see cref="MetodoPagoSiatCatalogo"/>, extendido con el flag
    /// <c>Activo</c> controlado por el operador (no por el SIN).
    ///
    /// El caché se refresca desde <c>SincronizadorCatUnidadMedida</c> cuando
    /// corre el sync (al boot del server + diario 08:10 BOT + manual vía
    /// <c>POST /api/catalogos/sincronizar-unidades-medida</c>).
    ///
    /// Thread-safety: el snapshot se reemplaza atómicamente vía
    /// <see cref="Volatile.Write"/> + lectura con <see cref="Volatile.Read"/>.
    /// </summary>
    public static class UnidadMedidaSiatCatalogo
    {
        /// <summary>
        /// Catálogo universal hardcoded con los 125 códigos que devuelve el
        /// SIN en <c>sincronizarParametricaUnidadMedida</c> (jun-2026, sync 12).
        /// Se usa como fallback antes del primer sync real contra el SIAT:
        /// el sistema sigue facturando aunque el sync falle o el endpoint
        /// no responda.
        ///
        /// Cobertura: TODOS los códigos del SIN, con su descripción canónica.
        /// Activo: solo los 9 códigos que la cafetería ya usa están marcados
        /// como <c>true</c>. El resto arranca en <c>false</c> hasta que el
        /// operador los habilite (vía SQL o el panel admin futuro).
        ///
        /// Por qué la clave es el código (int) y no la descripción (string):
        /// en el XML del SIN hay descripciones duplicadas (códigos 4 y 75
        /// ambos son "BOLSA", 57 y 58 ambos son "UNIDAD…", 60 y 101 son
        /// "YARDA"). Si la clave fuera la descripción, <c>Dictionary</c> /
        /// <c>ToDictionary</c> lanzarían <c>ArgumentException</c> al construir
        /// el snapshot — exactamente el bug de jun-2026 que rompía el type
        /// initializer del catálogo. Ver
        /// [[kafeyana-catalogo-typeinit-duplicate-keys]] para el detalle.
        ///
        /// Decisión del operador (2026-06-27): para el código 57 usamos
        /// "UNIDAD (BIENES)" (no "UNIDAD (SERVICIOS)" que es 58), porque la
        /// cafetería vende productos/bienes. TAZA, PORCION y PLATO caen al
        /// mismo código 57 por equivalencia operativa.
        /// </summary>
        private static readonly Dictionary<int, (string Descripcion, bool Activo)> CatalogoUniversalHardcoded =
            new()
            {
                // =========================
                // Activos: los 9 que la cafetería usa
                // =========================
                [57] = ("UNIDAD (BIENES)", true),       // antes "UNIDAD", elegido BIENES por decisión del operador
                [97] = ("VASO", true),                    // antes "VASO"
                [5]  = ("BOTELLAS", true),                // antes "BOTELLA" (singular) — el SIN lo trae en plural
                [6]  = ("CAJA", true),
                [17] = ("GRAMO", true),
                [28] = ("LITRO", true),
                [33] = ("MILIGRAMOS", true),              // antes "MILIGRAMO" — el SIN lo trae en plural
                [34] = ("MILILITRO", true),
                [62] = ("OTRO", true),

                // =========================
                // Inactivos: el resto del catálogo universal del SIN
                // =========================
                [1]   = ("BOBINAS", false),
                [2]   = ("BALDE", false),
                [3]   = ("BARRILES", false),
                [4]   = ("BOLSA", false),                // ⚠️ mismo texto que 75
                [7]   = ("CARTONES", false),
                [8]   = ("CENTIMETRO CUADRADO", false),
                [9]   = ("CENTIMETRO CUBICO", false),
                [10]  = ("CENTIMETRO LINEAL", false),
                [11]  = ("CIENTO DE UNIDADES", false),
                [12]  = ("CILINDRO", false),
                [13]  = ("CONOS", false),
                [14]  = ("DOCENA", false),
                [15]  = ("FARDO", false),
                [16]  = ("GALON INGLES", false),
                [18]  = ("GRUESA", false),
                [19]  = ("HECTOLITRO", false),
                [20]  = ("HOJA", false),
                [21]  = ("JUEGO", false),
                [22]  = ("KILOGRAMO", false),
                [23]  = ("KILOMETRO", false),
                [24]  = ("KILOVATIO HORA", false),
                [25]  = ("KIT", false),
                [26]  = ("LATAS", false),
                [27]  = ("LIBRAS", false),
                [29]  = ("MEGAWATT HORA", false),
                [30]  = ("METRO", false),
                [31]  = ("METRO CUADRADO", false),
                [32]  = ("METRO CUBICO", false),
                [35]  = ("MILIMETRO", false),
                [36]  = ("MILIMETRO CUADRADO", false),
                [37]  = ("MILIMETRO CUBICO", false),
                [38]  = ("MILLARES", false),
                [39]  = ("MILLON DE UNIDADES", false),
                [40]  = ("ONZAS", false),
                [41]  = ("PALETAS", false),
                [42]  = ("PAQUETE", false),
                [43]  = ("PAR", false),
                [44]  = ("PIES", false),
                [45]  = ("PIES CUADRADOS", false),
                [46]  = ("PIES CUBICOS", false),
                [47]  = ("PIEZAS", false),
                [48]  = ("PLACAS", false),
                [49]  = ("PLIEGO", false),
                [50]  = ("PULGADAS", false),
                [51]  = ("RESMA", false),
                [52]  = ("TAMBOR", false),
                [53]  = ("TONELADA CORTA", false),
                [54]  = ("TONELADA LARGA", false),
                [55]  = ("TONELADAS", false),
                [56]  = ("TUBOS", false),
                [58]  = ("UNIDAD (SERVICIOS)", false),  // ⚠️ mismo texto raíz que 57 — se desambigua por código
                [59]  = ("US GALON (3,7843 L)", false),
                [60]  = ("YARDA", false),               // ⚠️ mismo texto que 101
                [61]  = ("YARDA CUADRADA", false),
                [63]  = ("ONZA TROY", false),
                [64]  = ("LIBRA FINA", false),
                [65]  = ("DISPLAY", false),
                [66]  = ("BULTO", false),
                [67]  = ("DIAS", false),
                [68]  = ("MESES", false),
                [69]  = ("QUINTAL", false),
                [70]  = ("ROLLO", false),
                [71]  = ("HORAS", false),
                [72]  = ("AGUJA", false),
                [73]  = ("AMPOLLA", false),
                [74]  = ("BIDÓN", false),
                [75]  = ("BOLSA", false),               // ⚠️ mismo texto que 4
                [76]  = ("CAPSULA", false),
                [77]  = ("CARTUCHO", false),
                [78]  = ("COMPRIMIDO", false),
                [79]  = ("ESTUCHE", false),
                [80]  = ("FRASCO", false),
                [81]  = ("JERINGA", false),
                [82]  = ("MINI BOTELLA", false),
                [83]  = ("SACHET", false),
                [84]  = ("TABLETA", false),
                [85]  = ("TERMO", false),
                [86]  = ("TUBO", false),
                [87]  = ("BARRIL (EEUU) 60 F", false),
                [88]  = ("BARRIL [42 GALONES(EEUU)]", false),
                [89]  = ("METRO CUBICO 68F VOL", false),
                [90]  = ("MIL PIES CUBICOS 14696 PSI", false),
                [91]  = ("MIL PIES CUBICOS 14696 PSI 68FAH", false),
                [92]  = ("MILLAR DE PIES CUBICOS (1000 PC)", false),
                [93]  = ("MILLONES DE PIES CUBICOS (1000000 PC)", false),
                [94]  = ("MILLONES DE BTU (1000000 BTU)", false),
                [95]  = ("UNIDAD TERMICA BRITANICA (TI)", false),
                [96]  = ("POMO", false),
                [98]  = ("TETRAPACK", false),
                [99]  = ("CARTOLA", false),
                [100] = ("JABA", false),
                [101] = ("YARDA", false),                // ⚠️ mismo texto que 60
                [102] = ("BANDEJA", false),
                [103] = ("TURRIL", false),
                [104] = ("BLISTER", false),
                [105] = ("TIRA", false),
                [106] = ("MEGAWATT", false),
                [107] = ("KILOWATT", false),
                [108] = ("AMORTIZACION", false),
                [109] = ("OVULOS", false),
                [110] = ("SUPOSITORIOS", false),
                [111] = ("SOBRES", false),
                [112] = ("VIAL", false),
                [113] = ("HECTAREAS", false),
                [114] = ("ARROBA", false),
                [115] = ("AEROSOL", false),
                [116] = ("BARRA", false),
                [117] = ("CONJUNTO", false),
                [118] = ("FANEGA", false),
                [119] = ("PACK", false),
                [120] = ("PIPETA", false),
                [121] = ("POTE", false),
                [122] = ("PASTILLA", false),
                [123] = ("TONELADA METRICA", false),
                [124] = ("EQUIPOS", false),
                [125] = ("PIE TABLAR", false),
                [126] = ("KILATES", false),
            };

        /// <summary>
        /// Set de códigos que la cafetería ya usa (los del fallback activos).
        /// Se usa como seed default para sembrar <c>Activo=true</c> en el
        /// primer sync (cuando la BD aún no tiene el flag persistido) y como
        /// lookup rápido desde <see cref="Refrescar"/>.
        /// </summary>
        public static readonly IReadOnlySet<int> HardcodedActivosCodes =
            CatalogoUniversalHardcoded
                .Where(kv => kv.Value.Activo)
                .Select(kv => kv.Key)
                .ToHashSet();

        /// <summary>
        /// Snapshot inicial del catálogo (los 125 del SIN, 9 marcados activos).
        /// Se usa para inicializar <see cref="_snapshot"/> antes del primer sync
        /// real contra el SIAT y como reset desde <see cref="ResetearAFallback"/>.
        ///
        /// Se clona del <see cref="CatalogoUniversalHardcoded"/> para evitar que
        /// una mutación accidental del snapshot (ej. <c>_snapshot.Clear()</c>)
        /// contamine el fallback que también usa <c>ResetearAFallback</c>.
        /// </summary>
        private static readonly Dictionary<int, (string Descripcion, bool Activo)> SnapshotInicialFallback =
            new(CatalogoUniversalHardcoded);

        /// <summary>
        /// Snapshot inmutable del catálogo (código → (descripción canónica,
        /// activo)). Antes del primer sync se inicializa con
        /// <see cref="SnapshotInicialFallback"/>: los 125 códigos del SIN,
        /// 9 marcados como activos (los que la cafetería ya usa) y 116
        /// inactivos. El GET del backend filtra por <c>Activo=true</c> y solo
        /// expone los 9 al POS.
        /// </summary>
        private static volatile Dictionary<int, (string Descripcion, bool Activo)> _snapshot = SnapshotInicialFallback;

        /// <summary>
        /// True cuando el caché sigue siendo el fallback hardcoded (todavía
        /// no corrió ningún sync del SIAT en este proceso).
        /// </summary>
        public static bool EsFallback { get; private set; } = true;

        /// <summary>
        /// Limpia el snapshot al estado inicial (útil para tests).
        ///
        /// Reusa <see cref="SnapshotInicialFallback"/> (ya dedup'd) en lugar
        /// de llamar a <c>PorDescripcionFallback.ToDictionary(...)</c>, que
        /// lanzaría <c>ArgumentException</c> por las claves duplicadas del
        /// fallback original (TAZA/PORCION/PLATO → 57=UNIDAD).
        /// </summary>
        public static void ResetearAFallback()
        {
            Volatile.Write(ref _snapshot, SnapshotInicialFallback);
            EsFallback = true;
        }

        /// <summary>
        /// Reemplaza el snapshot del catálogo con los datos del SIN.
        ///
        /// Reglas de merge:
        ///   - Códigos nuevos del SIN que no estaban → arrancan <c>Activo=true</c>
        ///     si pertenecen al seed default (los <see cref="HardcodedActivosCodes"/>),
        ///     si no <c>Activo=false</c>.
        ///   - Códigos existentes → se actualiza solo <c>Descripcion</c>; el flag
        ///     <c>Activo</c> se PRESERVA (config del operador).
        ///   - Códigos que estaban en la BD pero ya no devuelve el SIN → se
        ///     MANTIENEN en el snapshot con su estado anterior (no se borran,
        ///     preserva auditoría).
        /// </summary>
        /// <param name="unidadesSiat">Lista cruda del SIN (codigo, descripcion).</param>
        /// <param name="estatusExistente">
        /// Mapa código → <c>Activo</c> actual en BD (puede ser null si el
        /// caller no lo tiene; en ese caso se aplica el seed default).
        /// </param>
        public static void Refrescar(
            IEnumerable<(int Codigo, string Descripcion)> unidadesSiat,
            IDictionary<int, bool>? estatusExistente = null)
        {
            var snapshotActual = Volatile.Read(ref _snapshot);
            var nuevo = new Dictionary<int, (string Descripcion, bool Activo)>(snapshotActual);

            foreach (var (codigo, descripcion) in unidadesSiat)
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
                    activo = HardcodedActivosCodes.Contains(codigo);
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

        /// <summary>
        /// Devuelve solo las unidades con <c>Activo=true</c> (las que el
        /// operador habilitó o los 9 hardcoded de <see cref="HardcodedActivosCodes"/>
        /// antes del primer sync). Esta es la fuente que consume el GET del
        /// backend (<c>/api/catalogos/unidades-medida</c>) y el POS para
        /// mostrar las opciones elegibles.
        ///
        /// Antes del fix de jun-2026 el catálogo tenía 12 entradas con 3
        /// duplicados por equivalencia (TAZA/PORCION/PLATO → 57). Eso causaba
        /// que el POS mostrara la misma opción 4 veces en el dropdown.
        /// </summary>
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
        /// usan los DTOs / preparers para rechazar ventas contra unidades
        /// deshabilitadas.
        /// </summary>
        public static bool EsValidoYActivo(int codigo)
        {
            return Volatile.Read(ref _snapshot).TryGetValue(codigo, out var v) && v.Activo;
        }

        /// <summary>
        /// Descripción canónica del código. Si no existe en el catálogo,
        /// devuelve <c>$"Unidad {codigo}"</c> en lugar de lanzar para no
        /// romper respuestas al cliente.
        /// </summary>
        public static string ObtenerDescripcion(int codigo) =>
            Volatile.Read(ref _snapshot).TryGetValue(codigo, out var v)
                ? v.Descripcion
                : $"Unidad {codigo}";

        /// Aliases para nombres históricos/coloquiales del frontend que no
        /// coinciden exactamente con la descripción canónica del SIN.
        /// Permite editar productos viejos (TAZA, PORCION, etc.) sin romper.
        private static readonly Dictionary<string, string> _aliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["UNIDAD"]  = "UNIDAD (BIENES)",
                ["TAZA"]    = "UNIDAD (BIENES)",
                ["PORCION"] = "UNIDAD (BIENES)",
                ["PLATO"]   = "UNIDAD (BIENES)",
                ["BOTELLA"] = "BOTELLAS",
                ["MILIGRAMO"] = "MILIGRAMOS",
            };

        /// <summary>
        /// Resuelve una descripción (ej. "UNIDAD", "VASO") al código SIN
        /// correspondiente. Solo resuelve si la unidad está activa en el
        /// catálogo vigente. Soporta aliases coloquiales (TAZA, PORCION…)
        /// para compatibilidad con productos guardados antes del catálogo SIAT.
        /// </summary>
        public static bool TryResolver(
            string descripcion,
            out int codigo,
            out string descripcionCanonica)
        {
            // Normalizar: si el input es un alias, usar la descripción canónica
            if (_aliases.TryGetValue(descripcion, out var canonica))
                descripcion = canonica;

            var snapshot = Volatile.Read(ref _snapshot);
            foreach (var kv in snapshot)
            {
                if (!kv.Value.Activo) continue;
                if (!string.Equals(
                        kv.Value.Descripcion,
                        descripcion,
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                codigo = kv.Key;
                descripcionCanonica = kv.Value.Descripcion;
                return true;
            }

            codigo = 0;
            descripcionCanonica = string.Empty;
            return false;
        }

        /// <summary>
        /// Resuelve una descripción hardcoded del fallback (la lista que la
        /// cafetería usa históricamente) al código SIN correspondiente, sin
        /// filtrar por <c>Activo</c>. Útil para constantes de default como
        /// "todo producto nuevo arranca en UNIDAD".
        ///
        /// Si el catálogo fue sincronizado por el SIAT y ese código ya no
        /// existe, devuelve 0 (el caller debe decidir el fallback).
        /// </summary>
        public static bool TryGetCodigo(string descripcion, out int codigo)
        {
            var snapshot = Volatile.Read(ref _snapshot);
            foreach (var kv in snapshot)
            {
                if (!string.Equals(
                        kv.Value.Descripcion,
                        descripcion,
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                codigo = kv.Key;
                return true;
            }

            codigo = 0;
            return false;
        }
    }
}