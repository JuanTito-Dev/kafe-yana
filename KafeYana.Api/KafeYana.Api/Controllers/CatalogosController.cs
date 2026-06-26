using KafeYana.Infrastructure.Servicios;
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

        public CatalogosController(
            SincronizadorCatActividades sincronizadorActividades,
            SincronizadorCatDocumentoSector sincronizadorDocumentosSector,
            SincronizadorCatMotivoAnulacion sincronizadorMotivosAnulacion,
            SincronizadorCatActividadDocumentoSector sincronizadorActividadesDocumentoSector,
            SincronizadorCatLeyenda sincronizadorLeyendas)
        {
            _sincronizadorActividades = sincronizadorActividades;
            _sincronizadorDocumentosSector = sincronizadorDocumentosSector;
            _sincronizadorMotivosAnulacion = sincronizadorMotivosAnulacion;
            _sincronizadorActividadesDocumentoSector = sincronizadorActividadesDocumentoSector;
            _sincronizadorLeyendas = sincronizadorLeyendas;
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
    }
}