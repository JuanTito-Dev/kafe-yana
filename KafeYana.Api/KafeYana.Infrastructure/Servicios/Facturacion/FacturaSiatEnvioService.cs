using KafeYana.Application.Dtos.FacturacionDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios.IFacturacion;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Facturacion;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios.SiatConnectivity;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios.Facturacion
{
    public class FacturaSiatEnvioService(
        IRecepcionFacturaService _recepcionFactura,
        IFacturaVentaSiatPreparer _facturaVentaSiatPreparer,
        IUnitWork _db,
        ISiatConnectivityMonitor _monitor,
        ILogger<FacturaSiatEnvioService> logger) : IFacturaSiatEnvioService
    {
        public async Task<ResultadoEnvioFacturaSiatDto> EnviarVentaAsync(
            Venta venta,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(venta.XmlBase64))
            {
                venta.EstadoSiat = FacturaEstado.Pendiente;
                venta.ErrorMensaje = "La venta no tiene archivo de factura para enviar al SIAT.";

                return new ResultadoEnvioFacturaSiatDto
                {
                    Enviado = false,
                    Transaccion = false,
                    EstadoSiat = venta.EstadoSiat,
                    ErrorMensaje = venta.ErrorMensaje
                };
            }

            // Si la venta nunca fue confirmada por el SIAT (nunca pasó por el
            // preparer con éxito), un rechazo o error de comunicación en este
            // intento debe dejarla otra vez en Facturado=false — así el próximo
            // reintento vuelve a pasar por el preparer (fecha/CUF/CUFD/XML
            // frescos) en vez de reenviar el XML viejo, y el guard de edición de
            // datos fiscales en ReenviarFacturaAsync no bloquea la corrección.
            // NO afecta al cobro normal con Factura=true (ahí Facturado ya es
            // true ANTES de entrar acá, por lo que eraNuncaFacturada es false).
            var eraNuncaFacturada = !venta.Facturado;

            try
            {
                // FIX #6/FIX #4 — si la venta llega sin preparar (Facturado=false),
                // corremos el preparer AQUÍ para que el flujo de rescate (que no
                // pasa por ReenviarFacturaAsync) regenere TODO con un CUFD vigente
                // del SIAT. Sin esto, la venta mantiene el CUF/CUFD viejos del
                // contingencia, y el SIAT rechaza con 1002 (CUF inválido) + 1003
                // (CUFD inválido) porque el CUFD vigente del SIAT ya cambió.
                // Ver [[kafeyana-contingencia-984-rescate]].
                //
                // En el flujo online normal de cobro, ConstruirVentaFacturadaAsync
                // ya dejó Facturado=true antes de llamar EnviarVentaAsync — este
                // branch SOLO se activa en rescates desatendidos.
                if (!venta.Facturado)
                {
                    logger.LogInformation(
                        "Venta {VentaId} sin preparar (Facturado=false) — llamando Preparer para regenerar CUFD/CUF/XML/Hash con el CUFD vigente del SIAT",
                        venta.Id);

                    await _facturaVentaSiatPreparer.PrepararVentaSinFacturarAsync(venta, ct);
                }

                var hash = string.IsNullOrWhiteSpace(venta.CodigoHash) ? null : venta.CodigoHash;

                // Diagnóstico: logueamos el CUFD y CUF que vamos a enviar para poder
                // correlacionar con el error 1002/1003 del SIAT si vuelve a fallar.
                logger.LogInformation(
                    "Enviando factura {NumeroFactura} (VentaId={VentaId}). Cuf={Cuf}, Cufd={Cufd}, Suc={Suc}, PV={PV}, TipoEmision={TipoEmision}",
                    venta.NumeroFactura, venta.Id, venta.Cuf, venta.Cufd,
                    venta.CodigoSucursal, venta.CodigoPuntoVenta, venta.TipoEmision);

                RespuestaRecepcionFacturaDto respuesta;

                // Ramas:
                // - TipoEmision=2 + EventoSignificativoSiatId poblado → contingencia offline.
                //   El sobre SOAP debe llevar codigoRecepcionEventoSignificativo; el CUFD/CUIS
                //   son los del momento del evento, cacheados en BD.
                // - Cualquier otro caso → flujo online normal con consistencia CUF/CUFD estricta.
                if (venta.TipoEmision == 2 && venta.EventoSignificativoSiatId is not null)
                {
                    var codigoRecepcionEvento = venta.EventoSignificativoSiat?.CodigoRecepcionEventoSignificativo;

                    // Fail-closed: si llegamos aquí sin el código de recepción del evento,
                    // es un bug del flujo offline. No dejamos pasar al SIAT sin ese código
                    // porque rechazaría con 1002/1003 de todos modos.
                    if (string.IsNullOrWhiteSpace(codigoRecepcionEvento))
                    {
                        logger.LogError(
                            "Venta contingencia (VentaId={VentaId}) sin codigoRecepcionEventoSignificativo en EventoSignificativoSiat. "
                          + "Reenvío abortado — corregir el evento en BD antes de reintentar.",
                            venta.Id);

                        venta.EstadoSiat = FacturaEstado.Pendiente;
                        venta.ErrorMensaje = "Falta codigoRecepcionEventoSignificativo en el evento asociado.";

                        return new ResultadoEnvioFacturaSiatDto
                        {
                            Enviado = false,
                            Transaccion = false,
                            EstadoSiat = venta.EstadoSiat,
                            ErrorMensaje = venta.ErrorMensaje
                        };
                    }

                    logger.LogInformation(
                        "Reenviando factura contingencia {NumeroFactura} (VentaId={VentaId}, EventoId={EventoId}, CodigoRecepcionEvento={CodRecEv})",
                        venta.NumeroFactura, venta.Id,
                        venta.EventoSignificativoSiatId, codigoRecepcionEvento);

                    // Gap 12: EnviarRecepcionContingenciaAsync ahora usa el método
                    // SOAP `recepcionPaqueteFactura` con 1 venta (porque el singular
                    // rechaza CodigoEmision=2 con error 916).
                    respuesta = await _recepcionFactura.EnviarRecepcionContingenciaAsync(
                        venta, venta.EventoSignificativoSiat!, ct);
                }
                else
                {
                    // Pasamos venta.Cufd + CodigoSucursal + CodigoPuntoVenta para que el
                    // sobre SOAP use EXACTAMENTE el mismo CUFD y PV con los que se
                    // construyó el CUF embebido en venta.Cuf. Si no, el servicio de
                    // recepción hace una llamada independiente a ObtenerCufdVigenteAsync
                    // que puede devolver un CUFD distinto (race por la tolerancia de 2s
                    // del CufdService), y el SIAT rechaza con 1002 (CUF inválido) +
                    // 1003 (CUFD inválido). Ver [[kafeyana-cuf-cufd-fechaemision]].
                    respuesta = await _recepcionFactura.EnviarRecepcionAsync(
                        venta.XmlBase64, hash, venta.FechaEmision,
                        venta.Cufd, venta.CodigoSucursal, venta.CodigoPuntoVenta, ct);
                }

                AplicarResultadoSiat(venta, respuesta);

                if (!respuesta.Transaccion)
                {
                    if (eraNuncaFacturada)
                        venta.Facturado = false;

                    logger.LogWarning(
                        "SIAT rechazó factura {NumeroFactura} (VentaId={VentaId}). {Error}",
                        venta.NumeroFactura,
                        venta.Id,
                        venta.ErrorMensaje);
                }
                else
                {
                    logger.LogInformation(
                        "Factura {NumeroFactura} (VentaId={VentaId}) validada por SIAT. CodigoRecepcion={Codigo}",
                        venta.NumeroFactura,
                        venta.Id,
                        venta.CodigoRecepcion);
                }

                return MapearResultado(venta, respuesta, enviado: true);
            }
            catch (SiatOfflineException ex)
            {
                // Circuito abierto: NO es un error, es una operación diferida.
                // La venta queda persistida con formato contingencia (TipoEmision=2)
                // y vinculada al evento significativo activo, para que
                // ReenvioFacturasContingenciaService la procese cuando el SIAT vuelva.
                venta.TipoEmision = 2;
                venta.EventoSignificativoSiatId = _monitor.ObtenerEventoActivo(
                    ex.CodigoSucursal, ex.CodigoPuntoVenta);
                venta.EstadoSiat = FacturaEstado.Pendiente;
                venta.ErrorMensaje = null;       // no es un error, es diferida
                venta.CodigoRecepcion = null;

                logger.LogInformation(
                    "Factura {NumeroFactura} (VentaId={VentaId}) diferida a contingencia. "
                  + "EventoSignificativoSiatId={EventoId}, Suc={Suc}, PV={PV}",
                    venta.NumeroFactura, venta.Id, venta.EventoSignificativoSiatId,
                    ex.CodigoSucursal, ex.CodigoPuntoVenta);

                return new ResultadoEnvioFacturaSiatDto
                {
                    Enviado = true,                 // frontend NO muestra error al cajero
                    Transaccion = false,            // el SIAT todavía no la validó
                    EstadoSiat = venta.EstadoSiat,
                    CodigoRecepcion = null,
                    ErrorMensaje = null
                };
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error de comunicación al enviar factura {NumeroFactura} (VentaId={VentaId}) al SIAT",
                    venta.NumeroFactura,
                    venta.Id);

                venta.EstadoSiat = FacturaEstado.Pendiente;
                venta.CodigoRecepcion = null;
                venta.ErrorMensaje = $"No se pudo enviar al SIAT: {ex.Message}";

                if (eraNuncaFacturada)
                    venta.Facturado = false;

                return new ResultadoEnvioFacturaSiatDto
                {
                    Enviado = false,
                    Transaccion = false,
                    EstadoSiat = venta.EstadoSiat,
                    ErrorMensaje = venta.ErrorMensaje
                };
            }
        }

        public async Task<ResultadoEnvioFacturaSiatDto> ReenviarFacturaAsync(
            int ventaId,
            DtoDatosFiscalesReenvio? datosFiscales = null,
            CancellationToken ct = default)
        {
            var venta = await _db.ventas.TraerVentaConDetallesAsync(ventaId);
            if (venta is null)
                throw new VentaException("Venta no encontrada.");

            if (venta.EstadoSiat == FacturaEstado.Anulada)
                throw new VentaException("No se puede reenviar al SIAT una venta anulada.");

            if (datosFiscales is not null)
            {
                // El guard es contra EstadoSiat==Validada (aceptada por el SIAT), no
                // contra Facturado a secas: una venta Observada/Pendiente/Rechazada
                // tiene Facturado=true (se intentó enviar) pero el SIAT NUNCA la
                // aceptó, así que corregir NIT/nombre y reenviar debe permitirse. Solo
                // una factura ya Validada requiere Nota de Ajuste para corregirse.
                if (venta.EstadoSiat == FacturaEstado.Validada)
                    throw new VentaException(
                        "No se pueden modificar los datos fiscales de una venta ya facturada. Use Nota de Ajuste.");

                var cliente = await ClientePedidoHelper.VincularClienteAlPedidoAsync(
                    _db, datosFiscales.Id_Cliente, datosFiscales.Nombre, datosFiscales.Dni);

                if (cliente is not null)
                {
                    if (!cliente.Dni.HasValue)
                        throw new VentaException("El cliente no tiene C.L. registrada.");

                    venta.NombreRazonSocial = cliente.Nombre;
                    venta.NumeroDocumento = cliente.Dni.Value.ToString();
                    venta.CodigoCliente = !string.IsNullOrWhiteSpace(cliente.Codigo)
                        ? cliente.Codigo
                        : ClienteCodigoService.Generar(cliente.Nombre, cliente.Id);
                    venta.CodigoTipoDocumentoIdentidad = datosFiscales.CodigoTipoDocumento
                        ?? venta.CodigoTipoDocumentoIdentidad;
                    venta.Complemento = string.IsNullOrWhiteSpace(datosFiscales.Complemento)
                        ? null
                        : datosFiscales.Complemento.Trim();
                }
            }

            if (venta.Facturado && venta.EstadoSiat == FacturaEstado.Validada)
            {
                return new ResultadoEnvioFacturaSiatDto
                {
                    Enviado = false,
                    Transaccion = true,
                    EstadoSiat = venta.EstadoSiat,
                    CodigoRecepcion = venta.CodigoRecepcion,
                    CodigoEstado = (int)FacturaEstado.Validada,
                    CodigoDescripcion = "La factura ya está validada por el SIAT.",
                    ErrorMensaje = null
                };
            }

            // FIX #7 — auto-rescate de huérfana por evento Rechazado (cascada 984).
            // Si la venta tiene TipoEmision=2 (contingencia) pero su evento asociado
            // está Rechazado (típicamente tras 984 cuando el monitor intentó reenviar
            // un AutomaticoSinSoap), el flujo normal de EnviarVentaAsync aborta con
            // fail-closed porque el evento no tiene CodigoRecepcionEventoSignificativo.
            // Detectamos esa condición ANTES de preparar y reclasificamos la venta a
            // online: TipoEmision=2→1, FK→null, Facturado=false. Luego el camino
            // estándar online regenera Cuf/Cufd/NumeroFactura/XmlBase64/CodigoHash y
            // emite vía recepcionFactura singular. Ver [[kafeyana-contingencia-984-rescate]].
            //
            // NO aplicamos el rescate si el evento está Activo (esperamos al monitor
            // a que registre OK) o Cerrado (debería estar Validada, requiere revisión).
            if (venta.TipoEmision == 2
                && venta.EventoSignificativoSiatId is not null
                && venta.EventoSignificativoSiat?.EstadoContingencia == EventoContingenciaEstado.Rechazado)
            {
                logger.LogWarning(
                    "FIX #7: Venta {VentaId} huérfana por evento {EventoId} Rechazado. "
                  + "Reclasificando a online (TipoEmision=1, FK=null, Facturado=false) "
                  + "y reenviando.",
                    venta.Id, venta.EventoSignificativoSiatId);

                venta.TipoEmision = 1;
                venta.EventoSignificativoSiatId = null;
                venta.Facturado = false;
                venta.ErrorMensaje = "FIX #7: rescate manual — evento Rechazado (984), reenvío online.";
            }

            if (!venta.Facturado)
            {
                await _facturaVentaSiatPreparer.PrepararVentaSinFacturarAsync(venta, ct);
            }
            else
            {
                // Facturado=true y llegamos hasta acá => EstadoSiat != Validada (el caso
                // Validada ya retornó arriba). Es Observada/Pendiente: el SIAT nunca
                // aceptó esta venta, así que regeneramos CUF/CUFD/XML/Hash — si el
                // cajero corrigió datos fiscales (guard arriba), el XML viejo tenía el
                // error; si fue un reenvío simple, esto solo refresca fecha/CUF/leyenda
                // sin costo (reusa CUFD en caché, ver [[kafeyana-cufd-1dia]]).
                await _facturaVentaSiatPreparer.RegenerarVentaObservadaAsync(venta, ct);
            }

            var resultado = await EnviarVentaAsync(venta, ct);
            await _db.SaveUnitWork();
            return resultado;
        }

        private static void AplicarResultadoSiat(Venta venta, RespuestaRecepcionFacturaDto respuesta)
        {
            if (respuesta.Transaccion)
            {
                venta.EstadoSiat = FacturaEstado.Validada;
                venta.CodigoRecepcion = respuesta.CodigoRecepcion;
                venta.ErrorMensaje = null;
                return;
            }

            venta.EstadoSiat = FacturaEstado.Observada;
            venta.CodigoRecepcion = null;
            venta.ErrorMensaje = FormatearErroresSiat(respuesta);
        }

        private static string FormatearErroresSiat(RespuestaRecepcionFacturaDto respuesta)
        {
            var errores = string.Join(" | ", respuesta.CodigosRespuesta
                .Select(m => $"[{m.Codigo}] {m.Descripcion}"));

            if (!string.IsNullOrWhiteSpace(errores))
                return errores;

            return respuesta.CodigoDescripcion ?? "El SIAT rechazó la factura sin detalle.";
        }

        private static ResultadoEnvioFacturaSiatDto MapearResultado(
            Venta venta,
            RespuestaRecepcionFacturaDto respuesta,
            bool enviado)
        {
            return new ResultadoEnvioFacturaSiatDto
            {
                Enviado = enviado,
                Transaccion = respuesta.Transaccion,
                EstadoSiat = venta.EstadoSiat,
                CodigoEstado = respuesta.Transaccion
                    ? respuesta.CodigoEstado ?? (int)FacturaEstado.Validada
                    : respuesta.CodigoEstado,
                CodigoRecepcion = venta.CodigoRecepcion,
                CodigoDescripcion = respuesta.CodigoDescripcion,
                ErrorMensaje = venta.ErrorMensaje,
                CodigosRespuesta = respuesta.CodigosRespuesta
            };
        }
    }
}
