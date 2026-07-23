using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities.Catalogos;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Infrastructure.Configuration;
using KafeYana.Infrastructure.Data;
using KafeYana.Infrastructure.Servicios.Facturacion.Utilidades;
using KafeYana.Infrastructure.SiatClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    /// <summary>
    /// Implementación del servicio de eventos significativos SIAT.
    /// Ver <see cref="IEventoSignificativoSiatService"/> para el contrato y
    /// [[kafeyana-contingencia-siat]] para el flujo normativo completo.
    /// </summary>
    public class EventoSignificativoSiatService : IEventoSignificativoSiatService
    {
        private readonly IUnitWork _db;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly SiatHttpClient _siat;
        private readonly ICufdService _cufdService;
        private readonly ICuisService _cuisService;
        private readonly IFechaHoraSiatService _fechaHoraSiat;
        private readonly SiatOptions _siatOpts;
        private readonly ILogger<EventoSignificativoSiatService> _logger;

        /// <summary>
        /// Ya NO se usa un placeholder (ej: 2 min): el SIAT rechaza ese rango
        /// con error 981 "RANGO DE FECHAS DE EVENTO SIGNIFICATIVO INVALIDO".
        /// Cuando el operador o el flujo automático no envía
        /// <c>fechaHoraFinEvento</c>, se persiste NULL y se setea con la hora
        /// real de recuperación en <see cref="ReenviarRegistroAsync"/>.
        /// </summary>
        private const string _placeholderDuracionNota =
            "Ver doc XML arriba: NO se usa más DefaultDuracionEvento.";

        public EventoSignificativoSiatService(
            IUnitWork db,
            IDbContextFactory<AppDbContext> dbFactory,
            SiatHttpClient siat,
            ICufdService cufdService,
            ICuisService cuisService,
            IFechaHoraSiatService fechaHoraSiat,
            IOptions<SiatOptions> siatOpts,
            ILogger<EventoSignificativoSiatService> logger)
        {
            _db = db;
            _dbFactory = dbFactory;
            _siat = siat;
            _cufdService = cufdService;
            _cuisService = cuisService;
            _fechaHoraSiat = fechaHoraSiat;
            _siatOpts = siatOpts.Value;
            _logger = logger;
        }

        public async Task<ResultadoRegistroEventoSignificativoDto> RegistrarEventoAsync(
            DtoRegistrarEventoSignificativo dto,
            CancellationToken ct = default)
        {
            ValidarDtoEstatico(dto);

            // Validación contra catálogo + auto-fill de descripción.
            // Es async porque toca BD (CatEventosSignificativos). El catálogo se
            // sincroniza diariamente del SIAT (SincronizacionEventoSignificativoHostedService);
            // si por alguna razón la BD está vacía (server recién instalado antes del
            // primer sync), la validación fallará con un mensaje claro para el operador.
            await ValidarContraCatalogoYAutoFillAsync(dto, ct);

            var codigoSucursal = dto.CodigoSucursal ?? _siatOpts.CodigoSucursal;
            var codigoPuntoVenta = dto.CodigoPuntoVenta ?? _siatOpts.CodigoPuntoVenta;
            var fechaInicio = (dto.FechaHoraInicioEvento ?? DateTime.UtcNow).ToUniversalTime();
            // Si el operador NO envía fechaHoraFinEvento, persistimos NULL.
            // El SIAT rechaza rangos placeholder (ej: +2min) con error 981.
            // La fecha real se setea en ReenviarRegistroAsync (Pieza 3b) si
            // la registración local fue sin SOAP, o se usa la fechaFin del
            // DTO si vino explícita.
            DateTime? fechaFin = dto.FechaHoraFinEvento?.ToUniversalTime();
            var origen = string.IsNullOrWhiteSpace(dto.Origen) ? "Manual" : dto.Origen.Trim();

            // No puede haber dos contingencias activas a la vez para el mismo PV.
            // Esto evita duplicar codigosRecepcion si el operador hace doble clic o si el
            // wrapper automático dispara mientras una manual está abierta.
            var contingenciaAbierta = await _db.eventosSignificativosSiat
                .ObtenerContingenciaActivaAsync(codigoSucursal, codigoPuntoVenta, ct);

            if (contingenciaAbierta is not null)
            {
                throw new VentaException(
                    $"Ya existe una contingencia activa para el PV (Suc={codigoSucursal}, PV={codigoPuntoVenta}). "
                  + $"CodigoRecepcion previo={contingenciaAbierta.CodigoRecepcionEventoSignificativo}. "
                  + "Cierre la contingencia actual antes de registrar una nueva.");
            }

            // Resolver CUIS + CUFD vigentes — los necesita el sobre SOAP.
            // esContingencia=true: en contingencia el SIAT no compara fechaEmision contra
            // hora actual, podemos reusar el CUFD durante sus 24h oficiales. Sin este flag
            // el sistema descarta el CUFD cada 5 min y dispara requests innecesarios que
            // pueden agotar el timeout (pérdida de paquetes contingencia).
            var cuis = await _cuisService.ObtenerCuisVigenteAsync(codigoSucursal, codigoPuntoVenta, ct);
            var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                codigoSucursal, codigoPuntoVenta, fechaInicio, ct, esContingencia: true);

            // Después de ValidarContraCatalogoYAutoFillAsync, Descripcion SIEMPRE
            // está poblada (auto-fill desde catálogo o el valor custom del caller).
            // Este null-check es defensivo contra una regresión futura.
            if (string.IsNullOrWhiteSpace(dto.Descripcion))
            {
                throw new InvalidOperationException(
                    "Descripcion vacía después de la validación contra catálogo. "
                  + "Es un bug: ValidarContraCatalogoYAutoFillAsync debería haber poblado el campo.");
            }
            var descripcionFinal = dto.Descripcion.Trim();

            var solicitudSiat = new SolicitudRegistroEventoSignificativoSiatDto
            {
                CodigoAmbiente = _siatOpts.CodigoAmbiente,
                CodigoPuntoVenta = codigoPuntoVenta,
                CodigoSistema = _siatOpts.CodigoSistema,
                CodigoSucursal = codigoSucursal,
                Cufd = cufd.Codigo,
                CufdEvento = cufd.Codigo,
                Cuis = cuis.Codigo,
                Descripcion = descripcionFinal,
                FechaHoraInicioEvento = fechaInicio,
                // Si el caller NO mandó fechaFin, no mandamos el evento al SIAT
                // en este path — debería haber usado RegistrarLocalmenteSinSoapAsync
                // para que Pieza 3b (ReenviarRegistroAsync) setee la hora real de
                // recuperación. Ver [[kafeyana-contingencia-siat]].
                FechaHoraFinEvento = fechaFin
                    ?? throw new InvalidOperationException(
                        "FechaHoraFinEvento es requerida para enviar al SIAT. "
                      + "Si la contingencia sigue activa al momento de este "
                      + "registro, use AutomaticoSinSoap (RegistrarYActivarAsync) "
                      + "y deje que Pieza 3b complete las fechas al recuperar el SIAT."),
                Nit = _siatOpts.Nit,
                CodigoMotivoEvento = dto.CodigoMotivo
            };

            _logger.LogInformation(
                "Registrando evento significativo SIAT: motivo={Motivo}, suc={Suc}, pv={PV}, inicio={Inicio}",
                dto.CodigoMotivo, codigoSucursal, codigoPuntoVenta, fechaInicio);

            var respuesta = await _siat.RegistroEventoSignificativoAsync(solicitudSiat, ct);

            // Persistir SIEMPRE — incluso si transaccion=false, queremos trazabilidad
            // del intento (auditoría + debugging).
            var entity = new EventoSignificativoSiat
            {
                CodigoMotivo = dto.CodigoMotivo,
                Descripcion = descripcionFinal,
                FechaHoraInicioEvento = fechaInicio,
                FechaHoraFinEvento = fechaFin,
                CodigoAmbiente = _siatOpts.CodigoAmbiente,
                CodigoPuntoVenta = codigoPuntoVenta,
                CodigoSucursal = codigoSucursal,
                CodigoSistema = _siatOpts.CodigoSistema,
                Nit = _siatOpts.Nit,
                Cufd = cufd.Codigo,
                CufdEvento = cufd.Codigo,
                // Hex de 15-16 chars que el SIAT embebió en el CUFD del momento
                // del corte. Se concatena al CUF de cada factura contingencia
                // (Gap 7). Distinto de Cufd (que es base64 y viaja como
                // parámetro SOAP del sobre de la factura contingencia).
                CodigoControlEvento = cufd.CodigoControl,
                Cuis = cuis.Codigo,
                CodigoRecepcionEventoSignificativo = respuesta.CodigoRecepcionEventoSignificativo,
                Transaccion = respuesta.Transaccion,
                CodigosRespuestaJson = JsonSerializer.Serialize(respuesta.CodigosRespuesta),
                Origen = origen,
                Usuario = string.IsNullOrWhiteSpace(dto.Usuario) ? null : dto.Usuario.Trim(),
                EstadoContingencia = respuesta.Transaccion
                    ? EventoContingenciaEstado.Activo
                    : EventoContingenciaEstado.Rechazado,
                FechaRegistro = DateTime.UtcNow,
                FechaCierre = respuesta.Transaccion ? null : DateTime.UtcNow
            };

            await _db.eventosSignificativosSiat.Crear(entity);
            await _db.SaveUnitWork();

            if (!respuesta.Transaccion)
            {
                _logger.LogWarning(
                    "SIAT rechazó registro de evento significativo. Motivo={Motivo}. {Errores}",
                    dto.CodigoMotivo,
                    string.Join(" | ", respuesta.CodigosRespuesta.Select(c => $"[{c.Codigo}] {c.Descripcion}")));

                throw new VentaException(
                    "El SIAT rechazó el registro del evento significativo: "
                  + (respuesta.CodigoDescripcion
                        ?? string.Join(" | ", respuesta.CodigosRespuesta.Select(c => $"[{c.Codigo}] {c.Descripcion}"))));
            }

            _logger.LogInformation(
                "Evento significativo registrado en SIAT. Id={Id}, CodigoRecepcion={Codigo}, suc={Suc}, pv={PV}",
                entity.Id, entity.CodigoRecepcionEventoSignificativo, codigoSucursal, codigoPuntoVenta);

            return new ResultadoRegistroEventoSignificativoDto
            {
                Transaccion = respuesta.Transaccion,
                CodigoRecepcionEventoSignificativo = respuesta.CodigoRecepcionEventoSignificativo,
                EventoId = entity.Id,
                FechaHoraInicioEvento = fechaInicio,
                FechaHoraFinEvento = fechaFin,
                EstadoContingencia = entity.EstadoContingencia,
                CodigoDescripcion = respuesta.CodigoDescripcion,
                CodigosRespuesta = respuesta.CodigosRespuesta
                    .Select(c => new CodigoRespuestaSiatDto
                    {
                        Codigo = c.Codigo,
                        Descripcion = c.Descripcion
                    }).ToList()
            };
        }

        public async Task<ResultadoRegistroEventoSignificativoDto> RegistrarYActivarAsync(
            int motivo,
            string origen,
            int codigoSucursal,
            int codigoPuntoVenta,
            string descripcion,
            CancellationToken ct = default)
        {
            // Wrapper del monitor automático. FIX #2: si SIAT está caído al cruzar
            // el umbral (escenario típico: el monitor abre contingencia cuando el
            // SinPingAsync regresa 4xx/5xx o timeout), RegistrarEventoAsync lanza
            // InvalidOperationException ANTES del SOAP (línea 138-143 exige
            // FechaHoraFinEvento no-null) y los catch del monitor (749-771) NO
            // tratan InvalidOperationException como "offline" → el evento nunca
            // quedaba persistido en BD y ObtenerEventoActivo devolvía null.
            //
            // Estrategia: intentar primero el path SOAP con fechaFin calculada
            // (modo en-línea). Si falla por conectividad (SiatOfflineException /
            // HttpRequestException / TaskCanceledException) o por el guard de
            // fechaFin-null cuando el caller NO proveyó duración, delegar a
            // RegistrarLocalmenteSinSoapAsync para que Pieza 3b complete los
            // campos al recuperar el SIAT. Ver [[kafeyana-contingencia-siat]].
            var fechaInicioLocal = DateTime.UtcNow;
            var fechaFinTentativa = fechaInicioLocal.AddMinutes(2); // SIAT rechaza rangos muy cortos con 981; 2 min es el mínimo práctico.

            var dto = new DtoRegistrarEventoSignificativo
            {
                CodigoMotivo = motivo,
                Origen = origen,
                CodigoSucursal = codigoSucursal,
                CodigoPuntoVenta = codigoPuntoVenta,
                Descripcion = descripcion,
                FechaHoraInicioEvento = fechaInicioLocal,
                FechaHoraFinEvento = fechaFinTentativa,
                Usuario = null
            };

            try
            {
                return await RegistrarEventoAsync(dto, ct);
            }
            catch (Exception ex) when (EsErrorDeConectividad(ex))
            {
                _logger.LogWarning(
                    "RegistrarYActivarAsync: SIAT no responde ({Tipo}). Cayendo a registro local sin SOAP. "
                  + "Motivo={Motivo}, suc={Suc}, pv={PV}",
                    ex.GetType().Name, motivo, codigoSucursal, codigoPuntoVenta);

                return await RegistrarLocalmenteSinSoapAsync(
                    motivo, origen, codigoSucursal, codigoPuntoVenta, descripcion, ct);
            }
        }

        /// <summary>
        /// Predicado centralizado: trata SIAT como caído cuando la excepción
        /// indica cortocircuito, fallo HTTP, timeout o cancelación de tarea.
        /// Cualquier otra excepción (Validación de catálogo, InvalidOperation por
        /// motivo fuera de rango, etc.) se propaga porque no es un problema de
        /// conectividad.
        /// </summary>
        private static bool EsErrorDeConectividad(Exception ex)
        {
            return ex is SiatOfflineException
                || ex is HttpRequestException
                || ex is TaskCanceledException;
        }

        public async Task<ResultadoRegistroEventoSignificativoDto> RegistrarLocalmenteSinSoapAsync(
            int motivo,
            string origen,
            int codigoSucursal,
            int codigoPuntoVenta,
            string descripcion,
            CancellationToken ct = default)
        {
            // Validación del rango del motivo (1..7). NO validamos contra el
            // catálogo CatEventosSignificativos porque queremos que esto funcione
            // aun si el sync diario nunca corrió (caso típico: server arrancó
            // con SIAT caído → BD de catálogo vacía). El operador puede corregir
            // el motivo más tarde si hace falta.
            if (motivo < 1 || motivo > 7)
            {
                throw new VentaException(
                    "CodigoMotivo debe estar entre 1 y 7 (catálogo SIAT). Motivos: "
                  + "1=CORTE INTERNET, 2=INACCESIBILIDAD WEB SIN, 3=ZONAS SIN INTERNET, "
                  + "4=VENTA SIN INTERNET, 5=VIRUS/SOFTWARE, 6=INFRA/HARDWARE, 7=CORTE ENERGÍA.");
            }

            // Defensa en profundidad: nunca debe haber dos contingencias activas
            // para el mismo (sucursal, puntoVenta). El monitor YA re-verifica
            // antes de llamar (DispararContingenciaAutomaticaAsync:250), pero si
            // este método se invoca desde otro path en el futuro, no debe romper
            // la invariante "una contingencia activa por PV".
            var contingenciaAbierta = await _db.eventosSignificativosSiat
                .ObtenerContingenciaActivaAsync(codigoSucursal, codigoPuntoVenta, ct);

            if (contingenciaAbierta is not null)
            {
                throw new VentaException(
                    $"Ya existe una contingencia activa para el PV (Suc={codigoSucursal}, PV={codigoPuntoVenta}). "
                  + $"CodigoRecepcion previo={contingenciaAbierta.CodigoRecepcionEventoSignificativo ?? "(sin SOAP)"}. "
                  + "Cierre la contingencia actual antes de registrar una nueva.");
            }

            var fechaInicio = DateTime.UtcNow;
            // Persistimos fechaFin=NULL en registro local sin SOAP. La fecha
            // real del cierre del evento se setea en ReenviarRegistroAsync
            // con la hora exacta de la recuperación, evitando el placeholder
            // de +2 min que el SIAT rechazaba con error 981.
            DateTime? fechaFin = null;

            // CUFD desde cache: si hay vigente, lo usamos; si hay vencido, lo
            // usamos también (modo contingencia degradado, log warning); si no
            // hay nada, persistimos con "" — Pieza 3b (ReenviarRegistroAsync)
            // obtendrá uno fresco al recuperar el SIAT antes de armar el SOAP.
            var cufdEnCache = await _cufdService.ObtenerCufdEnCacheAsync(
                codigoSucursal, codigoPuntoVenta, ct);

            var cufdEvento = cufdEnCache?.Codigo ?? string.Empty;
            if (string.IsNullOrEmpty(cufdEvento))
            {
                _logger.LogWarning(
                    "RegistrarLocalmenteSinSoapAsync: tabla Cufd vacía para PV ({Suc},{PV}). "
                  + "Persistiendo evento con CufdEvento='' — Pieza 3b obtendrá uno fresco.",
                    codigoSucursal, codigoPuntoVenta);
            }

            // Gap 7: capturar el CodigoControl del CUFD en cache (hex 15-16 chars)
            // para que VentaServices pueda construir un CUF válido en contingencia.
            // Si no hay CUFD en cache, queda null — sweep de Gap 7 marcará esas
            // ventas pre-fix con error si el operador llega a cobrar con ese
            // evento activo.
            var codigoControlEvento = cufdEnCache?.CodigoControl;
            if (string.IsNullOrEmpty(codigoControlEvento))
            {
                _logger.LogWarning(
                    "RegistrarLocalmenteSinSoapAsync: CUFD sin CodigoControl para PV ({Suc},{PV}). "
                  + "Eventuales ventas contingencia con este evento tendrán CUF malformado "
                  + "hasta que ReenviarRegistroAsync setee CodigoControlEvento.",
                    codigoSucursal, codigoPuntoVenta);
            }

            var entity = new EventoSignificativoSiat
            {
                CodigoMotivo = motivo,
                Descripcion = descripcion,
                FechaHoraInicioEvento = fechaInicio,
                FechaHoraFinEvento = fechaFin,
                CodigoAmbiente = _siatOpts.CodigoAmbiente,
                CodigoPuntoVenta = codigoPuntoVenta,
                CodigoSucursal = codigoSucursal,
                CodigoSistema = _siatOpts.CodigoSistema,
                Nit = _siatOpts.Nit,
                // NO se usó SOAP — se llenarán en ReenviarRegistroAsync (Pieza 3b).
                Cufd = string.Empty,
                CufdEvento = cufdEvento,
                CodigoControlEvento = codigoControlEvento,
                Cuis = string.Empty,
                CodigoRecepcionEventoSignificativo = null,
                Transaccion = false,
                CodigosRespuestaJson = "[]",
                Origen = "AutomaticoSinSoap",
                Usuario = null,
                EstadoContingencia = EventoContingenciaEstado.Activo,
                FechaRegistro = DateTime.UtcNow,
                FechaCierre = null
            };

            await _db.eventosSignificativosSiat.Crear(entity);
            await _db.SaveUnitWork();

            _logger.LogWarning(
                "Contingencia registrada LOCALMENTE sin SOAP (SIAT caído al cruce del umbral). "
              + "EventoId={Id}, Suc={Suc}, PV={PV}, Motivo={Motivo}, CufdEvento={Cufd}, Origen={Origen}",
                entity.Id, codigoSucursal, codigoPuntoVenta, motivo, cufdEvento, origen);

            return new ResultadoRegistroEventoSignificativoDto
            {
                Transaccion = false,
                CodigoRecepcionEventoSignificativo = null,
                EventoId = entity.Id,
                FechaHoraInicioEvento = fechaInicio,
                FechaHoraFinEvento = fechaFin,
                EstadoContingencia = EventoContingenciaEstado.Activo,
                CodigoDescripcion =
                    "Contingencia registrada localmente sin SOAP. Reenvío pendiente al recuperar el SIAT.",
                CodigosRespuesta = new List<CodigoRespuestaSiatDto>()
            };
        }

        public async Task<EstadoContingenciaDto> ObtenerEstadoContingenciaAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            var entity = await _db.eventosSignificativosSiat
                .ObtenerContingenciaActivaAsync(codigoSucursal, codigoPuntoVenta, ct);

            if (entity is null)
            {
                return new EstadoContingenciaDto
                {
                    ContingenciaActiva = false,
                    Origen = string.Empty
                };
            }

            string? descripcionMotivo = await ObtenerDescripcionMotivoAsync(entity.CodigoMotivo, ct);

            return new EstadoContingenciaDto
            {
                ContingenciaActiva = true,
                EventoSignificativoId = entity.Id,
                CodigoRecepcionEventoSignificativo = entity.CodigoRecepcionEventoSignificativo,
                CufdEvento = entity.CufdEvento,
                CodigoControlEvento = entity.CodigoControlEvento,
                CodigoMotivo = entity.CodigoMotivo,
                DescripcionMotivo = descripcionMotivo,
                FechaHoraInicioEvento = entity.FechaHoraInicioEvento,
                FechaHoraFinEvento = entity.FechaHoraFinEvento,
                CodigoSucursal = entity.CodigoSucursal,
                CodigoPuntoVenta = entity.CodigoPuntoVenta,
                Origen = entity.Origen
            };
        }

        public async Task<List<EventoSignificativoHistorialDto>> ListarHistorialAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            int limite,
            CancellationToken ct = default)
        {
            if (limite <= 0 || limite > 200) limite = 50;

            var entidades = await _db.eventosSignificativosSiat.Query()
                .Where(e => e.CodigoSucursal == codigoSucursal
                    && e.CodigoPuntoVenta == codigoPuntoVenta)
                .OrderByDescending(e => e.FechaRegistro)
                .Take(limite)
                .ToListAsync(ct);

            var motivos = await ListarMotivosCatalogoAsync(ct);
            var motivoMap = motivos.ToDictionary(m => m.Codigo, m => m.Descripcion);

            return entidades.Select(e => new EventoSignificativoHistorialDto
            {
                Id = e.Id,
                CodigoMotivo = e.CodigoMotivo,
                Descripcion = e.Descripcion,
                DescripcionMotivoCatalogo = motivoMap.GetValueOrDefault(e.CodigoMotivo),
                FechaHoraInicioEvento = e.FechaHoraInicioEvento,
                FechaHoraFinEvento = e.FechaHoraFinEvento,
                CodigoRecepcionEventoSignificativo = e.CodigoRecepcionEventoSignificativo,
                Transaccion = e.Transaccion,
                Origen = e.Origen,
                Usuario = e.Usuario,
                EstadoContingencia = e.EstadoContingencia,
                FechaRegistro = e.FechaRegistro,
                FechaCierre = e.FechaCierre
            }).ToList();
        }

        public async Task<List<ContingenciaActivaDto>> ListarContingenciasActivasAsync(
            CancellationToken ct = default)
        {
            var entidades = await _db.eventosSignificativosSiat
                .ListarContingenciasActivasAsync(ct);

            return entidades.Select(e => new ContingenciaActivaDto
            {
                EventoSignificativoId = e.Id,
                CodigoSucursal = e.CodigoSucursal,
                CodigoPuntoVenta = e.CodigoPuntoVenta,
                CodigoRecepcionEventoSignificativo = e.CodigoRecepcionEventoSignificativo,
                CodigoMotivo = e.CodigoMotivo,
                FechaHoraInicioEvento = e.FechaHoraInicioEvento,
                FechaHoraFinEvento = e.FechaHoraFinEvento,
                Origen = e.Origen
            }).ToList();
        }

        public async Task<ResultadoRegistroEventoSignificativoDto> ReenviarRegistroAsync(
            int eventoSignificativoId,
            CancellationToken ct = default)
        {
            var entity = await _db.eventosSignificativosSiat.FindByIdAsync(eventoSignificativoId);
            if (entity is null)
            {
                throw new VentaException(
                    $"EventoSignificativo Id={eventoSignificativoId} no existe en BD.");
            }

            // Idempotente: si ya tiene codigoRecepcion, no reenviar.
            if (!string.IsNullOrWhiteSpace(entity.CodigoRecepcionEventoSignificativo))
            {
                _logger.LogInformation(
                    "ReenviarRegistroAsync: evento {Id} ya tiene codigoRecepcion={Cod}. "
                  + "Idempotente — no se reenvía.",
                    entity.Id, entity.CodigoRecepcionEventoSignificativo);

                return new ResultadoRegistroEventoSignificativoDto
                {
                    Transaccion = entity.Transaccion,
                    CodigoRecepcionEventoSignificativo = entity.CodigoRecepcionEventoSignificativo,
                    EventoId = entity.Id,
                    FechaHoraInicioEvento = entity.FechaHoraInicioEvento,
                    FechaHoraFinEvento = entity.FechaHoraFinEvento,
                    EstadoContingencia = entity.EstadoContingencia,
                    CodigoDescripcion = "Ya registrado en SIAT previamente.",
                    CodigosRespuesta = new List<CodigoRespuestaSiatDto>()
                };
            }

            // Re-verificar contra BD por si ya está en estado terminal (ya sea
            // Cerrado-exitoso o Rechazado-por-SIAT). Ver [[kafeyana-contingencia-siat]].
            if (entity.EstadoContingencia == EventoContingenciaEstado.Cerrado
                || entity.EstadoContingencia == EventoContingenciaEstado.Rechazado)
            {
                throw new VentaException(
                    $"ReenviarRegistroAsync: evento {entity.Id} ya está en estado terminal "
                  + $"({entity.EstadoContingencia}). No se reenvía.");
            }

            // Resolver CUIS/CUFD frescos. Pieza 3b SÍ toca SIAT porque el monitor
            // ya detectó recuperación — sinó no estaríamos en este método.
            // El cortocircuito está bypaseado globalmente por el probe (ver
            // SiatConnectivityMonitor._bypassActivo), así que las llamadas
            // SOAP pasan sin necesidad de propagar flags por la cadena.
            // esContingencia=true: reenvío de registro SIEMPRE es contingencia.
            var cuis = await _cuisService.ObtenerCuisVigenteAsync(
                entity.CodigoSucursal, entity.CodigoPuntoVenta, ct);
            var cufd = await _cufdService.ObtenerCufdVigenteAsync(
                entity.CodigoSucursal, entity.CodigoPuntoVenta,
                entity.FechaHoraInicioEvento, ct, esContingencia: true);

            // Fix [984]: SIAT valida que cufdEvento sea el CUFD activo en FechaHoraInicioEvento.
            // entity.CufdEvento fue persistido por RegistrarLocalmenteSinSoapAsync desde la
            // cache en el momento exacto del corte — es el valor correcto.
            // Antes se usaba siempre cufd.Codigo (fresco); si el CUFD había rotado más de
            // 5 min después del corte (AntiguedadMaximaCufd online), ObtenerCufdVigenteAsync
            // emitía uno nuevo → SIAT rechazaba [984] porque ese CUFD no existía en FechaInicio.
            // El cufd fresco (sobre SOAP exterior) sigue siendo fresco; solo cufdEvento cambia.
            // Fallback al fresco solo si entity.CufdEvento está vacío (cache vacía al inicio del corte).
            var cufdEventoParaSoap = !string.IsNullOrEmpty(entity.CufdEvento)
                ? entity.CufdEvento
                : cufd.Codigo;

            // Si fechaFin venía NULL (registro local sin SOAP), setearla AHORA
            // con la hora OFICIAL del SIAT (no DateTime.UtcNow del backend,
            // que puede estar desincronizado y causar 981). Esto evita el
            // placeholder de +2 min histórico y mantiene coherencia con el
            // flujo online (VentaServices usa el mismo helper). Ver
            // [[kafeyana-contingencia-siat]].
            //
            // Gap 8: si SIAT muere entre el boot y este reenvío, fallback a
            // UtcNow con warning — no bloquear el flujo. La fechaInicio NO se
            // toca (queda con DateTime.UtcNow del registro local) para no
            // invalidar los CUFs ya emitidos por ConstruirVentaOfflineAsync.
            //
            // IMPORTANTE sobre Kind: IFechaHoraSiatService.ObtenerFechaHoraOficialAsync
            // devuelve DateTime con Kind=Unspecified (la hora BOT cruda que
            // devolvió el SIAT). Para el SOAP necesitamos exactamente esa hora
            // BOT (sin doble conversión que cause desfase). Para persistir en
            // PostgreSQL necesitamos Kind=Utc (Npgsql rechaza Unspecified en
            // timestamp with time zone). Por eso:
            //   - horaSiatBot (Kind=Unspecified) → SOAP y logs (BOT puro).
            //   - horaSiatUtc (Kind=Utc, +4h)    → entity.FechaHoraFinEvento.
            if (!entity.FechaHoraFinEvento.HasValue)
            {
                DateTime horaSiatBot;
                try
                {
                    horaSiatBot = await _fechaHoraSiat
                        .ObtenerFechaHoraOficialAsync(
                            entity.CodigoSucursal, entity.CodigoPuntoVenta, ct);

                    _logger.LogInformation(
                        "ReenviarRegistroAsync: evento {Id} tenía fechaFin=null. "
                      + "Seteando con hora oficial del SIAT: {FechaFin} "
                      + "(al serializar SOAP={BotIso}).",
                        entity.Id, horaSiatBot,
                        SiatFechaEmision.Formatear(horaSiatBot));
                }
                catch (Exception ex) when (ex is VentaException or HttpRequestException)
                {
                    _logger.LogWarning(ex,
                        "ReenviarRegistroAsync: no se pudo obtener hora oficial del SIAT. "
                      + "Fallback a DateTime.UtcNow (puede estar desincronizado vs SIAT).");
                    horaSiatBot = DateTime.UtcNow;
                }

                entity.FechaHoraFinEvento = SiatFechaEmision.ToUtcForDb(horaSiatBot);
            }

            var solicitudSiat = new SolicitudRegistroEventoSignificativoSiatDto
            {
                CodigoAmbiente = entity.CodigoAmbiente,
                CodigoPuntoVenta = entity.CodigoPuntoVenta,
                CodigoSistema = entity.CodigoSistema,
                CodigoSucursal = entity.CodigoSucursal,
                Cufd = cufd.Codigo,
                CufdEvento = cufdEventoParaSoap,
                Cuis = cuis.Codigo,
                Descripcion = entity.Descripcion,
                FechaHoraInicioEvento = entity.FechaHoraInicioEvento,
                FechaHoraFinEvento = entity.FechaHoraFinEvento.Value,  // ya no es null acá
                Nit = entity.Nit,
                CodigoMotivoEvento = entity.CodigoMotivo
            };

            _logger.LogInformation(
                "ReenviarRegistroAsync: enviando SOAP registroEventoSignificativo para evento {Id}. "
              + "Cufd fresco={CufdFresco} (VigenteHasta={VigenteHasta:yyyy-MM-ddTHH:mm:ss.fff}), "
              + "CufdEvento={CufdEv} (entity.CufdEventoPersistido era={CufdEvPersistido}), Cuis={Cuis}. "
              + "RangoEvento: [{Inicio:yyyy-MM-ddTHH:mm:ss.fff}, {Fin:yyyy-MM-ddTHH:mm:ss.fff}] "
              + "({DuracionMin:F1} min). Cubre VigenteHasta?={Cubre}",
                entity.Id,
                cufd.Codigo, cufd.FechaVigencia,
                cufdEventoParaSoap, entity.CufdEvento,
                cuis.Codigo,
                entity.FechaHoraInicioEvento, entity.FechaHoraFinEvento,
                (entity.FechaHoraFinEvento.Value - entity.FechaHoraInicioEvento).TotalMinutes,
                entity.FechaHoraFinEvento.Value <= cufd.FechaVigencia);

            // Gap 8: log de las fechas que salen al XML en formato BOT (lo que
            // ve SIAT). Si vuelve el 981, este log revela el dato exacto que
            // se está mandando — clave para diagnosticar desfases del reloj
            // backend vs SIAT.
            _logger.LogInformation(
                "ReenviarRegistroAsync: fechas en sobre SOAP para evento {Id}. "
              + "fechaHoraInicioEvento={Inicio} (Kind={InicioKind}) -> BOT={InicioBot}; "
              + "fechaHoraFinEvento={Fin} (Kind={FinKind}) -> BOT={FinBot}.",
                entity.Id,
                solicitudSiat.FechaHoraInicioEvento, solicitudSiat.FechaHoraInicioEvento.Kind,
                SiatFechaEmision.Formatear(solicitudSiat.FechaHoraInicioEvento),
                solicitudSiat.FechaHoraFinEvento, solicitudSiat.FechaHoraFinEvento.Kind,
                SiatFechaEmision.Formatear(solicitudSiat.FechaHoraFinEvento));

            var respuesta = await _siat.RegistroEventoSignificativoAsync(
                solicitudSiat, ct);

            // UPDATE entity existente (no INSERT).
            entity.Cufd = cufd.Codigo;
            entity.CufdEvento = cufdEventoParaSoap;
            // CodigoControlEvento NO se sobreescribe: fue seteado desde entity.CufdEvento
            // en RegistrarLocalmenteSinSoapAsync y las facturas contingencia ya calcularon
            // sus CUFs con ese hex. Reemplazarlo con el CodigoControl del CUFD fresco
            // dejaría la entidad inconsistente con los CUFs emitidos.
            entity.Cuis = cuis.Codigo;
            entity.CodigoRecepcionEventoSignificativo = respuesta.CodigoRecepcionEventoSignificativo;
            entity.Transaccion = respuesta.Transaccion;
            entity.CodigosRespuestaJson = JsonSerializer.Serialize(respuesta.CodigosRespuesta);

            // Si el SIAT rechazó el reenvío (ej: 981 fechas inválidas), persistir
            // como Rechazado — NO Cerrado — para que el monitor no rehidrate
            // este evento como pendiente en el próximo boot. Cerrado es sólo
            // para cierres EXITOSOS.
            if (!respuesta.Transaccion)
            {
                entity.EstadoContingencia = EventoContingenciaEstado.Rechazado;
                entity.FechaCierre = DateTime.UtcNow;
            }

            await _db.SaveUnitWork();

            if (!respuesta.Transaccion)
            {
                _logger.LogWarning(
                    "SIAT rechazó reenvío de evento significativo. EventoId={Id}, Motivo={Motivo}, Estado={Estado}. {Errores}",
                    entity.Id, entity.CodigoMotivo, entity.EstadoContingencia,
                    string.Join(" | ", respuesta.CodigosRespuesta.Select(c => $"[{c.Codigo}] {c.Descripcion}")));

                throw new VentaException(
                    "El SIAT rechazó el reenvío del evento significativo: "
                  + (respuesta.CodigoDescripcion
                        ?? string.Join(" | ", respuesta.CodigosRespuesta.Select(c => $"[{c.Codigo}] {c.Descripcion}"))));
            }

            _logger.LogInformation(
                "Evento significativo RE-ENVIADO al SIAT. EventoId={Id}, CodigoRecepcion={Cod}, "
              + "Suc={Suc}, PV={PV}",
                entity.Id, entity.CodigoRecepcionEventoSignificativo,
                entity.CodigoSucursal, entity.CodigoPuntoVenta);

            return new ResultadoRegistroEventoSignificativoDto
            {
                Transaccion = respuesta.Transaccion,
                CodigoRecepcionEventoSignificativo = respuesta.CodigoRecepcionEventoSignificativo,
                EventoId = entity.Id,
                FechaHoraInicioEvento = entity.FechaHoraInicioEvento,
                FechaHoraFinEvento = entity.FechaHoraFinEvento,
                EstadoContingencia = entity.EstadoContingencia,
                CodigoDescripcion = respuesta.CodigoDescripcion,
                CodigosRespuesta = respuesta.CodigosRespuesta
                    .Select(c => new CodigoRespuestaSiatDto
                    {
                        Codigo = c.Codigo,
                        Descripcion = c.Descripcion
                    }).ToList()
            };
        }

        public async Task CerrarContingenciaAsync(int eventoSignificativoId, CancellationToken ct = default)
        {
            await _db.eventosSignificativosSiat.CerrarContingenciaAsync(eventoSignificativoId, ct);

            _logger.LogInformation(
                "Contingencia cerrada. EventoSignificativoId={Id}", eventoSignificativoId);
        }

        /// <summary>
        /// Auto-expira un evento activo porque su <c>FechaHoraInicio</c>
        /// excede <c>HorasMaximaContingenciaAbierta</c> (Gap 6). El SIN lo
        /// rechazaría con error 981 si intentamos reenviarlo, así que es
        /// mejor marcarlo terminal acá.
        ///
        /// Idempotente: si el evento ya está en estado terminal, no hace nada
        /// y devuelve 0 (defensa contra race conditions con otros flujos).
        ///
        /// Marca también las ventas Pendientes asociadas con un mensaje claro
        /// para que el operador las pueda identificar y actuar (anular o
        /// desvincular).
        /// </summary>
        public async Task<int> AutoExpirarEventoAsync(
            int eventoSignificativoId,
            CancellationToken ct = default)
        {
            var entity = await _db.eventosSignificativosSiat.FindByIdAsync(eventoSignificativoId)
                ?? throw new VentaException(
                    $"AutoExpirarEventoAsync: evento {eventoSignificativoId} no existe");

            // Idempotente: si ya está terminal, no hacer nada.
            if (entity.EstadoContingencia != EventoContingenciaEstado.Activo)
            {
                _logger.LogInformation(
                    "AutoExpirarEventoAsync: evento {Id} ya está {Estado} (terminal). No se re-procesa.",
                    entity.Id, entity.EstadoContingencia);
                return 0;
            }

            var ahora = DateTime.UtcNow;
            entity.EstadoContingencia = EventoContingenciaEstado.AutoExpirado;
            entity.FechaCierre = ahora;
            entity.CodigosRespuestaJson = JsonSerializer.Serialize(new[]
            {
                new
                {
                    codigo = 981,
                    descripcion = $"RANGO DE FECHAS DE EVENTO SIGNIFICATIVO INVALIDO "
                                + $"(auto-expirado por monitor: FechaHoraInicio="
                                + $"{entity.FechaHoraInicioEvento:O} excede "
                                + $"{_siatOpts.HorasMaximaContingenciaAbierta}h)"
                }
            });

            // Marcar ventas Pendientes asociadas con ErrorMensaje claro.
            var mensajeError =
                $"Contingencia auto-expirada por el monitor (evento {entity.Id} excedió "
              + $"{_siatOpts.HorasMaximaContingenciaAbierta}h sin cerrarse). "
              + "El SIN rechazó con 981 RANGO DE FECHAS DE EVENTO SIGNIFICATIVO INVALIDO. "
              + "Requiere acción manual del operador: anular o desvincular la venta.";

            var ventasAfectadas = await _db.ventas
                .MarcarVentasContingenciaExpiradaAsync(entity.Id, mensajeError, ct);

            await _db.SaveUnitWork();

            _logger.LogInformation(
                "AutoExpirarEventoAsync: evento {Id} auto-expirado. "
              + "Ventas Pendientes marcadas con error: {Count}.",
                entity.Id, ventasAfectadas);

            return ventasAfectadas;
        }

        // ─── Helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Validación rápida sincrónica del DTO: rango del CodigoMotivo y
        /// coherencia de fechas. La validación contra el catálogo real del
        /// SIAT (CatEventosSignificativos) se hace después en
        /// <see cref="ValidarContraCatalogoYAutoFillAsync"/> porque requiere BD.
        /// </summary>
        private static void ValidarDtoEstatico(DtoRegistrarEventoSignificativo dto)
        {
            if (dto is null)
                throw new VentaException("El cuerpo de la solicitud es requerido.");

            if (dto.CodigoMotivo < 1 || dto.CodigoMotivo > 7)
                throw new VentaException(
                    "CodigoMotivo es requerido y debe estar entre 1 y 7 (CatEventosSignificativos del SIN). "
                  + "Motivos vigentes: 1=CORTE INTERNET, 2=INACCESIBILIDAD WEB SIN, "
                  + "3=INGRESO A ZONAS SIN INTERNET, 4=VENTA EN LUGARES SIN INTERNET, "
                  + "5=VIRUS O FALLA SOFTWARE, 6=CAMBIO INFRAESTRUCTURA O FALLA HARDWARE, "
                  + "7=CORTE ENERGÍA ELÉCTRICA.");

            // Descripcion es OPCIONAL: si llega vacía se auto-rellena con la
            // descripción oficial del catálogo para ese motivo (más adelante).
            // Solo validamos el largo máximo si vino alguna descripción custom.
            if (!string.IsNullOrWhiteSpace(dto.Descripcion) && dto.Descripcion.Length > 500)
                throw new VentaException("Descripcion no puede superar los 500 caracteres.");

            if (dto.FechaHoraInicioEvento is DateTime inicio
                && dto.FechaHoraFinEvento is DateTime fin
                && fin < inicio)
            {
                throw new VentaException(
                    "FechaHoraFinEvento no puede ser anterior a FechaHoraInicioEvento.");
            }
        }

        /// <summary>
        /// Valida que el <c>CodigoMotivo</c> exista en el catálogo sincronizado
        /// del SIAT (<c>CatEventosSignificativos</c>) y, si <c>Descripcion</c>
        /// viene vacía, la rellena con la descripción oficial del catálogo.
        /// Esto permite al frontend enviar <c>{codigoMotivo: 4}</c> y obtener
        /// "VENTA EN LUGARES SIN INTERNET" automáticamente.
        ///
        /// Si el catálogo no tiene ese código (ej: el SIN agregó un motivo 8
        /// y aún no sincronizó), rechaza con mensaje claro para que el operador
        /// sepa qué pasó.
        /// </summary>
        private async Task ValidarContraCatalogoYAutoFillAsync(
            DtoRegistrarEventoSignificativo dto,
            CancellationToken ct)
        {
            var motivos = await ListarMotivosCatalogoAsync(ct);
            var motivoCatalogo = motivos.FirstOrDefault(m => m.Codigo == dto.CodigoMotivo);

            if (motivoCatalogo is null)
            {
                var codigosDisponibles = string.Join(", ", motivos.Select(m => m.Codigo));
                throw new VentaException(
                    $"CodigoMotivo={dto.CodigoMotivo} no existe en el catálogo CatEventosSignificativos "
                  + $"sincronizado del SIN. Códigos disponibles: [{codigosDisponibles}]. "
                  + "Si acaba de agregarse un motivo nuevo al SIN, espere al próximo sync diario.");
            }

            // Auto-fill: si la descripción viene vacía, usamos la del catálogo.
            // Si vino una descripción custom, la respetamos (el operador puede
            // agregar contexto adicional — ej: "VIRUS INFORMÁTICO — nodo 3 infectado").
            if (string.IsNullOrWhiteSpace(dto.Descripcion))
            {
                dto.Descripcion = motivoCatalogo.Descripcion;
            }
        }

        private async Task<string?> ObtenerDescripcionMotivoAsync(int codigoMotivo, CancellationToken ct)
        {
            var motivos = await ListarMotivosCatalogoAsync(ct);
            return motivos.FirstOrDefault(m => m.Codigo == codigoMotivo)?.Descripcion;
        }

        private async Task<List<CatEventoSignificativo>> ListarMotivosCatalogoAsync(CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.CatEventosSignificativos
                .AsNoTracking()
                .OrderBy(c => c.Codigo)
                .ToListAsync(ct);
        }
    }
}