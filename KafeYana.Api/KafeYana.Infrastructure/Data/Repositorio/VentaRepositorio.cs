using KafeYana.Application.Exceptions.Usuarios;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class VentaRepositorio : GenericRepositorio<Venta>, IVentaRepositorio
    {
        public VentaRepositorio(AppDbContext _db) : base(_db)
        {
        }

        public IQueryable<Venta> VentaQuery()
        {
            return _db.Ventas.AsNoTracking().AsQueryable();
        }

        public async Task<int> ContarVentasDelAnio(int anio)
        {
            return await _db.Ventas
                .Where(x => x.FechaEmision.Year == anio)
                .CountAsync();
        }

        /// <summary>
        /// Correlativo SIAT atómico vía sequence de Postgres.
        /// Reemplaza el MAX(NumeroFactura) + 1 que sufría race condition bajo
        /// cobros concurrentes (dos requests leían el mismo MAX → mismo NumeroFactura
        /// → colisión en IX_Venta_NumeroFactura). La sequence es atómica por
        /// diseño: cada nextval() devuelve un valor único e irrepetible.
        ///
        /// REQUISITO: la BD debe tener la sequence "Venta_NumeroFactura_seq".
        /// Si no existe, crearla con:
        ///   CREATE SEQUENCE IF NOT EXISTS "Venta_NumeroFactura_seq" START 1;
        /// (ajustar START al MAX(NumeroFactura)+1 actual de la tabla para no
        ///  duplicar números ya emitidos al SIAT).
        /// </summary>
        public async Task<long> SiguienteNumeroFacturaSiatAsync()
        {
            var result = await _db.Database
                .SqlQueryRaw<long>("SELECT nextval('\"Venta_NumeroFactura_seq\"')")
                .ToListAsync();

            return result[0];
        }

        public async Task<long> SiguienteNumeroFacturaCafcAsync()
        {
            var result = await _db.Database
                .SqlQueryRaw<long>("SELECT nextval('\"Venta_NumeroFacturaCafc_seq\"')")
                .ToListAsync();

            return result[0];
        }

        public async Task<Venta?> TraerVentaConDetallesAsync(int id)
        {
            return await _db.Ventas
                .Include(v => v.Detalles)
                .Include(v => v.EventoSignificativoSiat)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<List<Venta>> BuscarPendientesPorEventoAsync(
            int eventoSignificativoId,
            CancellationToken ct = default)
        {
            return await _db.Ventas
                .Include(v => v.EventoSignificativoSiat)
                .Where(v => v.EventoSignificativoSiatId == eventoSignificativoId
                    && v.Facturado
                    && v.EstadoSiat == FacturaEstado.Pendiente)
                .OrderBy(v => v.FechaEmision)
                .ToListAsync(ct);
        }

        /// <summary>
        /// Vincula al evento las ventas del "período gris" que quedaron
        /// Pendiente sin EventoSignificativoSiatId durante el cruce del
        /// umbral de fallos. Ver interfaz para semántica completa.
        /// </summary>
        public async Task<int> VincularVentasPendientesAlEventoAsync(
            int eventoSignificativoId,
            DateTime fechaInicioEvento,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            return await _db.Ventas
                .Where(v =>
                    v.EstadoSiat == FacturaEstado.Pendiente
                    && v.EventoSignificativoSiatId == null
                    && v.CodigoSucursal == codigoSucursal
                    && v.CodigoPuntoVenta == codigoPuntoVenta
                    && v.FechaEmision >= fechaInicioEvento)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.TipoEmision, 2)
                    .SetProperty(v => v.EventoSignificativoSiatId, (int?)eventoSignificativoId)
                    .SetProperty(v => v.ErrorMensaje, (string?)null),
                    ct);
        }

        /// <summary>
        /// Marca con ErrorMensaje las ventas Pendientes asociadas a un evento
        /// contingencia auto-expirado por el monitor (Gap 6). NO toca ventas
        /// ya Validadas/Anuladas/Observadas. UPDATE bulk sin tracking.
        /// </summary>
        public async Task<int> MarcarVentasContingenciaExpiradaAsync(
            int eventoSignificativoId,
            string mensajeError,
            CancellationToken ct = default)
        {
            return await _db.Ventas
                .Where(v => v.EventoSignificativoSiatId == eventoSignificativoId
                         && v.EstadoSiat == FacturaEstado.Pendiente)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.ErrorMensaje, mensajeError),
                    ct);
        }

        /// <summary>
        /// Gap 7: sweep de ventas contingencia con CUF malformado por el bug pre-fix
        /// (concatenaba CUFD base64 en lugar del CodigoControl hex). Solo TipoEmision=2,
        /// solo las que aún no tienen ErrorMensaje (idempotente). Las marcadas con
        /// error requieren intervención manual del operador (anular o desvincular).
        /// </summary>
        public async Task<int> MarcarVentasCufMalformadoAsync(
            string mensajeError,
            CancellationToken ct = default)
        {
            return await _db.Ventas
                .Where(v => v.TipoEmision == 2
                         && v.EstadoSiat == FacturaEstado.Pendiente
                         && v.ErrorMensaje == null
                         && (v.Cuf.Length < 50
                             || v.Cuf.Length > 80
                             || v.Cuf.Contains("==")))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.ErrorMensaje, mensajeError),
                    ct);
        }

        /// <summary>
        /// FIX #4 — sweep de ventas contingencia huérfanas (TipoEmision=2 + FK=null).
        /// Ver interfaz para el detalle del bug.
        /// </summary>
        public async Task<List<Venta>> BuscarPendientesSinEventoAsync(
            CancellationToken ct = default)
        {
            return await _db.Ventas
                .Include(v => v.Detalles)
                .Where(v => v.TipoEmision == 2
                         && v.EstadoSiat == FacturaEstado.Pendiente
                         && v.EventoSignificativoSiatId == null)
                .OrderBy(v => v.FechaEmision)
                .ToListAsync(ct);
        }

        /// <summary>
        /// FIX #6 — ventas contingencia pendientes vinculadas a un evento específico.
        /// Espejo de <see cref="BuscarPendientesSinEventoAsync"/> pero filtrando por
        /// FK poblada con el id del evento (rechazado vía 984).
        /// Ver interfaz para semántica completa.
        /// </summary>
        public async Task<List<Venta>> BuscarPendientesPorEventoIdAsync(
            int eventoId,
            CancellationToken ct = default)
        {
            return await _db.Ventas
                .Include(v => v.Detalles)
                .Where(v => v.TipoEmision == 2
                         && v.EventoSignificativoSiatId == eventoId
                         && v.Facturado
                         && v.EstadoSiat == FacturaEstado.Pendiente)
                .OrderBy(v => v.FechaEmision)
                .ToListAsync(ct);
        }
    }
}
