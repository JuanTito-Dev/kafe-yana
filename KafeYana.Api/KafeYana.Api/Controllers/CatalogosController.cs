using KafeYana.Infrastructure.Servicios;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    /// <summary>
    /// Endpoints de diagnóstico para la sincronización de catálogos del SIAT.
    ///
    /// El panel de certificación del SIAT consulta estos endpoints
    /// durante las "Pruebas Correctas" (Casos 1 y 2) para verificar
    /// que el sistema está listo y mantener los datos actualizados.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogosController : ControllerBase
    {
        private readonly SincronizadorCatActividades _sincronizadorActividades;
        private readonly SincronizadorCatDocumentoSector _sincronizadorDocumentosSector;
        private readonly SincronizadorCatMotivoAnulacion _sincronizadorMotivosAnulacion;
        private readonly SincronizadorCatActividadDocumentoSector _sincronizadorActividadesDocumentoSector;
        private readonly SincronizadorCatLeyenda _sincronizadorLeyendas;
        private readonly SincronizadorCodigosSiat _sincronizadorCodigosSiat;
        private readonly SincronizadorCatEventoSignificativo _sincronizadorEventoSignificativo;
        private readonly SincronizadorCatPaisOrigen _sincronizadorPaisOrigen;
        private readonly SincronizadorCatTipoDocumentoIdentidad _sincronizadorTipoDocumentoIdentidad;
        private readonly SincronizadorCatTipoEmision _sincronizadorTipoEmision;
        private readonly SincronizadorCatMetodosPago _sincronizadorMetodosPago;
        private readonly SincronizadorCatUnidadMedida _sincronizadorUnidadMedida;
        private readonly IUnitWork _unitWork;

        public CatalogosController(
            SincronizadorCatActividades sincronizadorActividades,
            SincronizadorCatDocumentoSector sincronizadorDocumentosSector,
            SincronizadorCatMotivoAnulacion sincronizadorMotivosAnulacion,
            SincronizadorCatActividadDocumentoSector sincronizadorActividadesDocumentoSector,
            SincronizadorCatLeyenda sincronizadorLeyendas,
            SincronizadorCodigosSiat sincronizadorCodigosSiat,
            SincronizadorCatEventoSignificativo sincronizadorEventoSignificativo,
            SincronizadorCatPaisOrigen sincronizadorPaisOrigen,
            SincronizadorCatTipoDocumentoIdentidad sincronizadorTipoDocumentoIdentidad,
            SincronizadorCatTipoEmision sincronizadorTipoEmision,
            SincronizadorCatMetodosPago sincronizadorMetodosPago,
            SincronizadorCatUnidadMedida sincronizadorUnidadMedida,
            IUnitWork unitWork)
        {
            _sincronizadorActividades = sincronizadorActividades;
            _sincronizadorDocumentosSector = sincronizadorDocumentosSector;
            _sincronizadorMotivosAnulacion = sincronizadorMotivosAnulacion;
            _sincronizadorActividadesDocumentoSector = sincronizadorActividadesDocumentoSector;
            _sincronizadorLeyendas = sincronizadorLeyendas;
            _sincronizadorCodigosSiat = sincronizadorCodigosSiat;
            _sincronizadorEventoSignificativo = sincronizadorEventoSignificativo;
            _sincronizadorPaisOrigen = sincronizadorPaisOrigen;
            _sincronizadorTipoDocumentoIdentidad = sincronizadorTipoDocumentoIdentidad;
            _sincronizadorTipoEmision = sincronizadorTipoEmision;
            _sincronizadorMetodosPago = sincronizadorMetodosPago;
            _sincronizadorUnidadMedida = sincronizadorUnidadMedida;
            _unitWork = unitWork;
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-actividades
        ///
        /// Ejecuta la sincronización del catálogo de actividades contra
        /// el SIAT de forma síncrona y devuelve { transaccion: true }
        /// cuando completa. La BD se reemplaza en una transacción atómica.
        /// </summary>
        [HttpPost("sincronizar-actividades")]
        public async Task<IActionResult> SincronizarActividades(CancellationToken ct)
        {
            try
            {
                var cantidad = await _sincronizadorActividades.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad
                });
            }
            catch (InvalidOperationException ex)
            {
                // El SIAT rechazó la operación (transaccion=false o SOAP Fault).
                // Devolvemos 502 Bad Gateway para que el panel del SIAT sepa
                // que la dependencia externa falló.
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-documentos-sector
        ///
        /// Ejecuta la sincronización del catálogo de documentos sectoriales
        /// (sincronizarParametricaTipoDocumentoSector) contra el SIAT de forma
        /// síncrona. Devuelve { transaccion: true, cantidad: N } cuando completa.
        /// </summary>
        [HttpPost("sincronizar-documentos-sector")]
        public async Task<IActionResult> SincronizarDocumentosSector(CancellationToken ct)
        {
            try
            {
                var cantidad = await _sincronizadorDocumentosSector.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-motivos-anulacion
        ///
        /// Ejecuta la sincronización del catálogo paramétrico de motivos de anulación
        /// (sincronizarParametricaMotivoAnulacion) contra el SIAT. Itera todos los
        /// PuntosVentaSiat activos, usa la primera respuesta exitosa para reemplazar
        /// la tabla maestra CatMotivosAnulacion y actualiza el caché en memoria
        /// usado por las validaciones de anulación.
        /// </summary>
        [HttpPost("sincronizar-motivos-anulacion")]
        public async Task<IActionResult> SincronizarMotivosAnulacion(CancellationToken ct)
        {
            try
            {
                var (cantidad, pvsExitosos) = await _sincronizadorMotivosAnulacion.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad,
                    pvsExitosos = pvsExitosos
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/catalogos/motivos-anulacion
        ///
        /// Devuelve el catálogo de motivos de anulación actualmente en uso por el backend
        /// (caché en memoria <c>MotivoAnulacionSiatCatalogo.ObtenerTodos()</c>).
        ///
        /// Esta es la fuente de verdad que consumen:
        ///   - Las validaciones de <c>DtoAnularFactura</c> / <c>DtoAnularNotaAjuste</c>.
        ///   - Los servicios <c>FacturaSiatAnulacionService</c> / <c>NotaAjusteSiatAnulacionService</c>.
        ///   - El frontend (modales de anulación de factura y nota C/D **y el modal
        ///     de emisión de nota C/D**, que reutiliza este mismo catálogo porque
        ///     el SIAT no expone una paramétrica separada para motivos de emisión).
        ///
        /// El caché se refresca automáticamente vía el hosted service diario a las 08:10 BOT
        /// o manualmente vía <c>POST /api/catalogos/sincronizar-motivos-anulacion</c>.
        ///
        /// <c>sincronizado = false</c> indica que el server arrancó pero el sync del SIAT
        /// todavía no corrió — los códigos que devuelve el <c>items</c> son los del
        /// <c>FallbackHardcoded</c> (pueden NO coincidir con los oficiales del SIN).
        /// La UI debe mostrar un aviso y bloquear el submit en ese caso.
        /// </summary>
        [HttpGet("motivos-anulacion")]
        public IActionResult GetMotivosAnulacion()
        {
            var cache = MotivoAnulacionSiatCatalogo.ObtenerTodos();
            var items = cache
                .Select(kvp => new { codigo = kvp.Key, descripcion = kvp.Value })
                .OrderBy(x => x.codigo)
                .ToList();

            return Ok(new
            {
                items,
                sincronizado = !MotivoAnulacionSiatCatalogo.EsFallback
            });
        }

        /// <summary>
        /// GET /api/catalogos/paises-origen
        ///
        /// Devuelve el catálogo paramétrico de países de origen del SIAT
        /// (tabla <c>CatPaisOrigen</c>, sincronizada por
        /// <c>SincronizadorCatPaisOrigen</c>). Cada item incluye el código SIN
        /// (1..211 según catálogo vigente) y la descripción oficial
        /// (ej. "BOLIVIA (ESTADO PLURINACIONAL DE)").
        ///
        /// Esta es la fuente de verdad que consume el frontend:
        ///   - <c>usePaisesOrigen</c> hook
        ///   - <c>DatosFiscalesForm</c> dropdown de país (visible sólo cuando
        ///     el tipo de documento es CEX=2 o PAS=3)
        ///
        /// <c>sincronizado = false</c> indica que el server arrancó pero el sync
        /// del SIAT todavía no corrió — los códigos que devuelve <c>items</c>
        /// pueden estar incompletos. La UI debe mostrar un aviso y bloquear el
        /// submit en ese caso.
        ///
        /// El catálogo se refresca automáticamente vía el hosted service diario
        /// a las 08:10 BOT o manualmente vía
        /// <c>POST /api/catalogos/sincronizar-paises-origen</c>.
        /// </summary>
        [HttpGet("paises-origen")]
        public async Task<IActionResult> GetPaisesOrigen(CancellationToken ct)
        {
            var paises = await _unitWork.catPaisesOrigen.GetAllOrderedAsync();
            var items = paises
                .Select(p => new { codigo = p.Codigo, descripcion = p.Descripcion })
                .ToList();

            // sincronizado=true en cuanto hay al menos un país cargado
            // (la respuesta del SIAT trae ~211 entradas; si llega 0 todavía
            // no corrió el primer sync).
            return Ok(new
            {
                items,
                sincronizado = items.Count > 0
            });
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-actividades-documento-sector
        ///
        /// Ejecuta la sincronización de la matriz Actividad ↔ Documento Sector
        /// (sincronizarListaActividadesDocumentoSector) contra el SIAT de forma
        /// síncrona. Itera todos los PuntosVentaSiat activos, usa la primera
        /// respuesta exitosa para reemplazar la tabla maestra
        /// CatActividadesDocumentosSector y marca UltimaSyncActividadesDocumentoSector
        /// en los PVs que devolvieron OK.
        /// </summary>
        [HttpPost("sincronizar-actividades-documento-sector")]
        public async Task<IActionResult> SincronizarActividadesDocumentoSector(CancellationToken ct)
        {
            try
            {
                var cantidad = await _sincronizadorActividadesDocumentoSector.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-leyendas
        ///
        /// Ejecuta la sincronización del catálogo de leyendas obligatorias del SIAT
        /// (sincronizarListaLeyendasFactura) contra el SIAT de forma síncrona.
        /// Itera todos los PuntosVentaSiat activos, usa la primera respuesta
        /// exitosa, la FILTRA por la actividad económica principal del operador
        /// y reemplaza la tabla maestra CatLeyendas. Marca UltimaSyncLeyendas
        /// en los PVs que devolvieron OK.
        /// </summary>
        [HttpPost("sincronizar-leyendas")]
        public async Task<IActionResult> SincronizarLeyendas(CancellationToken ct)
        {
            try
            {
                var (cantidad, pvsExitosos) = await _sincronizadorLeyendas.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad,
                    pvsExitosos = pvsExitosos
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-productos-servicios
        ///
        /// Ejecuta la sincronización del catálogo de productos/servicios del SIAT
        /// (sincronizarListaProductosServicios) contra el SIAT de forma síncrona.
        /// Itera todos los PuntosVentaSiat activos, usa la primera respuesta
        /// exitosa, la FILTRA por la actividad económica principal del operador
        /// y reemplaza la tabla CodigosSiat (alimenta el modal CodigoSinModal del
        /// frontend). Marca UltimaSyncCodigosSiat en los PVs que devolvieron OK.
        /// </summary>
        [HttpPost("sincronizar-productos-servicios")]
        public async Task<IActionResult> SincronizarProductosServicios(CancellationToken ct)
        {
            try
            {
                var (cantidad, pvsExitosos) = await _sincronizadorCodigosSiat.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad,
                    pvsExitosos = pvsExitosos
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-eventos-significativos
        ///
        /// Ejecuta la sincronización del catálogo paramétrico de eventos significativos
        /// del SIAT (sincronizarParametricaEventosSignificativos) contra el SIAT de
        /// forma síncrona. Itera todos los PuntosVentaSiat activos, usa la primera
        /// respuesta exitosa para reemplazar la tabla maestra CatEventosSignificativos
        /// (catálogo universal, no se filtra por actividad económica) y marca
        /// UltimaSyncEventosSignificativos en los PVs que devolvieron OK.
        ///
        /// Fuera de scope: el uso real de este catálogo para facturación en
        /// contingencia (registro del evento, buffer offline, paquete) se
        /// implementa en un ticket separado.
        /// </summary>
        [HttpPost("sincronizar-eventos-significativos")]
        public async Task<IActionResult> SincronizarEventosSignificativos(CancellationToken ct)
        {
            try
            {
                var (cantidad, pvsExitosos) = await _sincronizadorEventoSignificativo.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad,
                    pvsExitosos = pvsExitosos
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-paises-origen
        ///
        /// Ejecuta la sincronización del catálogo paramétrico de países de origen del SIAT
        /// (sincronizarParametricaPaisOrigen) contra el SIAT de forma síncrona. Itera
        /// todos los PuntosVentaSiat activos, usa la primera respuesta exitosa para
        /// reemplazar la tabla maestra CatPaisesOrigen (catálogo universal, no se filtra
        /// por actividad económica) y marca UltimaSyncPaisOrigen en los PVs que
        /// devolvieron OK.
        ///
        /// Fuera de scope: el uso real de este catálogo (factura de exportación,
        /// clientes extranjeros con pasaporte/CIE) se implementa en tickets separados.
        /// </summary>
        [HttpPost("sincronizar-paises-origen")]
        public async Task<IActionResult> SincronizarPaisesOrigen(CancellationToken ct)
        {
            try
            {
                var (cantidad, pvsExitosos) = await _sincronizadorPaisOrigen.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad,
                    pvsExitosos = pvsExitosos
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-tipos-documento-identidad
        ///
        /// Ejecuta la sincronización del catálogo paramétrico de tipos de documento
        /// de identidad del SIAT (sincronizarParametricaTipoDocumentoIdentidad) contra
        /// el SIAT de forma síncrona. Itera todos los PuntosVentaSiat activos, usa la
        /// primera respuesta exitosa para reemplazar la tabla maestra
        /// CatTiposDocumentoIdentidad (catálogo universal, no se filtra por actividad
        /// económica), refresca el caché estático usado por las validaciones de
        /// <c>codigoTipoDocumentoIdentidad</c> en cada venta y marca
        /// UltimaSyncTipoDocumentoIdentidad en los PVs que devolvieron OK.
        /// </summary>
        [HttpPost("sincronizar-tipos-documento-identidad")]
        public async Task<IActionResult> SincronizarTiposDocumentoIdentidad(CancellationToken ct)
        {
            try
            {
                var (cantidad, pvsExitosos) = await _sincronizadorTipoDocumentoIdentidad.SincronizarAsync(ct);

                var desdeBaseDatos = false;
                if (pvsExitosos == 0)
                {
                    // SIAT no respondió en ningún PV: usar la última sync exitosa
                    // persistida en BD en vez de dejar el catálogo en fallback hardcodeado.
                    desdeBaseDatos = await _sincronizadorTipoDocumentoIdentidad.IntentarCargarDesdeBaseDatosAsync(ct);
                }

                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad,
                    pvsExitosos = pvsExitosos,
                    desdeBaseDatos = desdeBaseDatos
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/catalogos/tipos-documento-identidad
        ///
        /// Devuelve el catálogo paramétrico de tipos de documento de identidad del SIAT
        /// (caché en memoria <c>TipoDocumentoIdentidadSiatCatalogo.ObtenerTodos()</c>).
        ///
        /// Esta es la fuente de verdad que consume:
        ///   - <c>DtoVentaPedido.Validate</c>: valida que <c>CodigoTipoDocumento</c>
        ///     esté en el catálogo vigente.
        ///   - <c>FacturaXmlGenerator</c> / <c>NotaAjusteXmlGenerator</c>: serializa
        ///     el código en el XML de la factura y la nota.
        ///   - <c>FacturaTicketBuilder</c>: arma la etiqueta del ticket impreso.
        ///
        /// El caché se refresca automáticamente vía el hosted service diario a las
        /// 08:10 BOT o manualmente vía
        /// <c>POST /api/catalogos/sincronizar-tipos-documento-identidad</c>.
        ///
        /// <c>sincronizado = false</c> indica que el server arrancó pero el sync del
        /// SIAT todavía no corrió — los códigos que devuelve el <c>items</c> son los
        /// del <c>FallbackHardcoded</c> (coinciden con los oficiales del SIN vigente
        /// a jun-2026, pero no se garantiza que sigan así). La UI debe mostrar un
        /// aviso si necesita estrictamente el catálogo del SIN.
        /// </summary>
        [HttpGet("tipos-documento-identidad")]
        public IActionResult GetTiposDocumentoIdentidad()
        {
            var cache = TipoDocumentoIdentidadSiatCatalogo.ObtenerTodos();
            var items = cache
                .Select(kvp => new { codigo = kvp.Key, descripcion = kvp.Value })
                .OrderBy(x => x.codigo)
                .ToList();

            return Ok(new
            {
                items,
                sincronizado = !TipoDocumentoIdentidadSiatCatalogo.EsFallback,
                desdeBaseDatos = TipoDocumentoIdentidadSiatCatalogo.EsDesdeBaseDatos
            });
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-tipos-emision
        ///
        /// Ejecuta la sincronización del catálogo paramétrico de tipos de emisión
        /// del SIAT (sincronizarParametricaTipoEmision) contra el SIAT de forma
        /// síncrona. Itera todos los PuntosVentaSiat activos, usa la primera
        /// respuesta exitosa para reemplazar la tabla maestra CatTiposEmision
        /// (catálogo universal, no se filtra por actividad económica), refresca
        /// el caché estático usado para validar el valor hardcoded
        /// <c>SiatOptions.CodigoEmision</c> y marca <c>UltimaSyncTipoEmision</c>
        /// en los PVs que devolvieron OK.
        /// </summary>
        [HttpPost("sincronizar-tipos-emision")]
        public async Task<IActionResult> SincronizarTiposEmision(CancellationToken ct)
        {
            try
            {
                var (cantidad, pvsExitosos) = await _sincronizadorTipoEmision.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    cantidad = cantidad,
                    pvsExitosos = pvsExitosos
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/catalogos/tipos-emision
        ///
        /// Devuelve el catálogo paramétrico de tipos de emisión del SIAT
        /// (caché en memoria <c>TipoEmisionSiatCatalogo.ObtenerTodos()</c>).
        ///
        /// Esta es la fuente de verdad que valida que el valor hardcoded
        /// <c>SiatOptions.CodigoEmision</c> siga siendo oficial (hoy = 1 = EN LINEA).
        ///
        /// El caché se refresca automáticamente vía el hosted service diario a las
        /// 08:10 BOT o manualmente vía
        /// <c>POST /api/catalogos/sincronizar-tipos-emision</c>.
        ///
        /// <c>sincronizado = false</c> indica que el server arrancó pero el sync del
        /// SIAT todavía no corrió — los códigos que devuelve el <c>items</c> son los
        /// del <c>FallbackHardcoded</c> (coinciden con los oficiales del SIN vigente
        /// a jun-2026, pero no se garantiza que sigan así).
        /// </summary>
        [HttpGet("tipos-emision")]
        public IActionResult GetTiposEmision()
        {
            var cache = TipoEmisionSiatCatalogo.ObtenerTodos();
            var items = cache
                .Select(kvp => new { codigo = kvp.Key, descripcion = kvp.Value })
                .OrderBy(x => x.codigo)
                .ToList();

            return Ok(new
            {
                items,
                sincronizado = !TipoEmisionSiatCatalogo.EsFallback
            });
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-metodos-pago
        ///
        /// Ejecuta la sincronización del catálogo paramétrico de métodos de pago
        /// del SIAT (sincronizarParametricaTipoMetodoPago) contra el SIAT de
        /// forma síncrona. Itera todos los PuntosVentaSiat activos, usa la
        /// primera respuesta exitosa y hace MERGE con la tabla maestra
        /// <c>CatMetodosPago</c> (catálogo universal, no se filtra por
        /// actividad económica).
        ///
        /// **Diferencia con los otros sync**: este catálogo tiene un flag
        /// <c>Activo</c> controlado por el operador. El merge PRESERVA el
        /// estado de <c>Activo</c> (los códigos nuevos arrancan en false salvo
        /// 1=EFECTIVO y 7=TRANSFERENCIA que arrancan activos).
        ///
        /// NO se ejecuta diario (solo al boot + manual). El catálogo tiene
        /// ~308 entradas y cambia muy poco.
        /// </summary>
        [HttpPost("sincronizar-metodos-pago")]
        public async Task<IActionResult> SincronizarMetodosPago(CancellationToken ct)
        {
            try
            {
                var (total, nuevos, actualizados, pvsExitosos) =
                    await _sincronizadorMetodosPago.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    total,
                    nuevos,
                    actualizados,
                    pvsExitosos
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/catalogos/metodos-pago
        ///
        /// Devuelve el catálogo paramétrico de métodos de pago del SIAT
        /// actualmente persistido en BD (sincronizado por
        /// <c>SincronizadorCatMetodosPago</c>, ~308 entradas). Solo devuelve
        /// los métodos con <c>Activo=true</c> (los que el operador habilitó).
        ///
        /// Esta es la fuente de verdad que consume:
        ///   - <c>DtoPagos.Validate</c>: valida que cada línea del body tenga
        ///     un código válido Y activo.
        ///   - <c>VentaServices.ResolverLineasYPagoPrincipal</c>: arma las
        ///     entidades <c>VentaPago</c> por cada venta.
        ///   - El frontend (<c>PagoPanel</c>) para mostrar solo los métodos
        ///     que el sistema acepta.
        ///
        /// <c>sincronizado = false</c> indica que el server arrancó pero el
        /// sync contra el SIAT todavía no corrió o falló en todos los PVs — en
        /// ese caso <c>items</c> viene del <c>FallbackHardcoded</c> (1=EFECTIVO,
        /// 2=TARJETA, 5=OTROS, 7=TRANSFERENCIA). La UI debe mostrar un aviso.
        ///
        /// El catálogo se refresca automáticamente al boot del server o
        /// manualmente vía
        /// <c>POST /api/catalogos/sincronizar-metodos-pago</c>.
        /// </summary>
        [HttpGet("metodos-pago")]
        public IActionResult GetMetodosPago()
        {
            var cache = MetodoPagoSiatCatalogo.ObtenerActivos();
            var items = cache
                .Select(kvp => new
                {
                    codigo = kvp.Key,
                    descripcion = kvp.Value,
                    activo = true
                })
                .OrderBy(x => x.codigo)
                .ToList();

            return Ok(new
            {
                items,
                sincronizado = !MetodoPagoSiatCatalogo.EsFallback
            });
        }

        /// <summary>
        /// POST /api/catalogos/sincronizar-unidades-medida
        ///
        /// Ejecuta la sincronización del catálogo paramétrico de unidades de medida
        /// del SIAT (<c>sincronizarParametricaUnidadMedida</c>) contra el SIAT de
        /// forma síncrona. Itera todos los PuntosVentaSiat activos, usa la primera
        /// respuesta exitosa y hace MERGE con la tabla maestra
        /// <c>CatUnidadesMedida</c> (catálogo universal, no se filtra por
        /// actividad económica).
        ///
        /// **Diferencia con los sync 1..10**: este catálogo tiene un flag
        /// <c>Activo</c> controlado por el operador. El merge PRESERVA el
        /// estado de <c>Activo</c> (los códigos nuevos arrancan en false salvo
        /// los 9 hardcoded {57=UNIDAD, 97=VASO, 5=BOTELLA, 6=CAJA,
        /// 33=MILIGRAMO, 17=GRAMO, 28=LITRO, 34=MILILITRO, 62=OTRO} que
        /// arrancan activos).
        ///
        /// Corre diario a las 08:10 BOT (espejo de <c>CatTipoEmision</c>).
        /// </summary>
        [HttpPost("sincronizar-unidades-medida")]
        public async Task<IActionResult> SincronizarUnidadesMedida(CancellationToken ct)
        {
            try
            {
                var (total, nuevos, actualizados, pvsExitosos) =
                    await _sincronizadorUnidadMedida.SincronizarAsync(ct);
                return Ok(new
                {
                    transaccion = true,
                    total,
                    nuevos,
                    actualizados,
                    pvsExitosos
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    transaccion = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/catalogos/unidades-medida
        ///
        /// Devuelve el catálogo paramétrico de unidades de medida del SIAT
        /// actualmente persistido en BD (sincronizado por
        /// <c>SincronizadorCatUnidadMedida</c>, ~50–100 entradas). Solo
        /// devuelve las unidades con <c>Activo=true</c> (las que el operador
        /// habilitó — por default los 9 hardcoded que la cafetería ya usaba).
        ///
        /// Esta es la fuente de verdad que consume:
        ///   - <c>VentaServices</c>: valida que el <c>UnidadMedida</c> de cada
        ///     detalle esté activo.
        ///   - <c>FacturaVentaSiatPreparer</c>: misma validación antes de
        ///     armar el sobre SOAP al SIAT.
        ///   - El frontend (<c>ProductForm</c>, <c>ElaboradoWizard</c>) para
        ///     mostrar solo las unidades que el sistema acepta.
        ///
        /// <c>sincronizado = false</c> indica que el server arrancó pero el
        /// sync contra el SIAT todavía no corrió o falló en todos los PVs —
        /// en ese caso <c>items</c> viene del <c>FallbackHardcoded</c> con
        /// los 12 pares originales. La UI debe mostrar un aviso.
        ///
        /// El catálogo se refresca automáticamente al boot del server + diario
        /// a las 08:10 BOT, o manualmente vía
        /// <c>POST /api/catalogos/sincronizar-unidades-medida</c>.
        /// </summary>
        [HttpGet("unidades-medida")]
        public IActionResult GetUnidadesMedida()
        {
            var cache = UnidadMedidaSiatCatalogo.ObtenerActivos();
            var items = cache
                .Select(kvp => new
                {
                    codigo = kvp.Key,
                    descripcion = kvp.Value,
                    activo = true
                })
                .OrderBy(x => x.codigo)
                .ToList();

            return Ok(new
            {
                items,
                sincronizado = !UnidadMedidaSiatCatalogo.EsFallback
            });
        }
    }
}