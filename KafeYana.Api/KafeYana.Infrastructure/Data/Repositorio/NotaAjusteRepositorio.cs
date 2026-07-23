using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class NotaAjusteRepositorio : GenericRepositorio<NotaAjuste>, INotaAjusteRepositorio
    {
        public NotaAjusteRepositorio(AppDbContext db) : base(db)
        {
        }

        public IQueryable<NotaAjuste> NotaAjusteQuery()
        {
            return _db.NotasAjuste.AsNoTracking().AsQueryable();
        }

        /// <summary>
        /// Correlativo SIAT atómico vía sequence de Postgres.
        /// Reemplaza el MAX(NumeroNotaCreditoDebito) + 1 que sufría race condition
        /// bajo emisiones concurrentes de notas de ajuste.
        ///
        /// REQUISITO: la BD debe tener la sequence "NotaAjuste_Numero_seq".
        /// Si no existe, crearla con:
        ///   CREATE SEQUENCE IF NOT EXISTS "NotaAjuste_Numero_seq" START 1;
        /// (ajustar START al MAX(NumeroNotaCreditoDebito)+1 actual de la tabla).
        /// </summary>
        public async Task<long> SiguienteNumeroNotaCreditoDebitoAsync()
        {
            var result = await _db.Database
                .SqlQueryRaw<long>("SELECT nextval('\"NotaAjuste_Numero_seq\"')")
                .ToListAsync();

            return result[0];
        }

        public async Task<NotaAjuste?> TraerNotaAjusteConDetallesAsync(int id)
        {
            return await _db.NotasAjuste
                .Include(n => n.Detalles)
                .Include(n => n.EventoSignificativoSiat)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<IReadOnlyList<NotaAjuste>> ListarPorVentaAsync(int ventaId)
        {
            return await _db.NotasAjuste
                .Where(n => n.IdVenta == ventaId)
                .OrderByDescending(n => n.FechaEmision)
                .ToListAsync();
        }

        public async Task<List<NotaAjuste>> BuscarPendientesPorEventoAsync(
            int eventoSignificativoId,
            CancellationToken ct = default)
        {
            return await _db.NotasAjuste
                .Include(n => n.EventoSignificativoSiat)
                .Where(n => n.EventoSignificativoSiatId == eventoSignificativoId
                    && n.EstadoSiat == FacturaEstado.Pendiente)
                .OrderBy(n => n.FechaEmision)
                .ToListAsync(ct);
        }

        /// <summary>
        /// Vincula al evento las notas del "período gris" que quedaron
        /// Pendiente sin EventoSignificativoSiatId durante el cruce del
        /// umbral de fallos. Ver interfaz para semántica completa.
        /// </summary>
        public async Task<int> VincularNotasPendientesAlEventoAsync(
            int eventoSignificativoId,
            DateTime fechaInicioEvento,
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            return await _db.NotasAjuste
                .Where(n =>
                    n.EstadoSiat == FacturaEstado.Pendiente
                    && n.EventoSignificativoSiatId == null
                    && n.CodigoSucursal == codigoSucursal
                    && n.CodigoPuntoVenta == codigoPuntoVenta
                    && n.FechaEmision >= fechaInicioEvento)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.TipoEmision, (int?)2)
                    .SetProperty(n => n.EventoSignificativoSiatId, (int?)eventoSignificativoId)
                    .SetProperty(n => n.ErrorMensaje, (string?)null),
                    ct);
        }

        /// <summary>
        /// Suma las cantidades devueltas por producto en notas VÁLIDAS (SIAT).
        /// Sólo se contabilizan las líneas con CodigoDetalleTransaccion = 2
        /// (la línea trans=1 del par SIAT es sólo referencia semántica del item
        /// original; sumar ambas duplicaría).
        ///
        /// Coherente con la regla del frontend
        /// (<c>frontend/src/pages/sales/sales.mapper.ts:85-86</c>), que sólo
        /// considera notas en estado "Validada" para calcular el saldo efectivo.
        /// </summary>
        public async Task<System.Collections.Generic.Dictionary<int, decimal>> ObtenerCantidadDevueltaPorDetallePagoAsync(int ventaId)
        {
            return await _db.NotasAjuste
                .AsNoTracking()
                .Where(n => n.IdVenta == ventaId
                         && n.EstadoSiat == Domain.TiposDeDatos.FacturaEstado.Validada)
                .SelectMany(n => n.Detalles)
                .Where(d => d.CodigoDetalleTransaccion == 2)
                .GroupBy(d => d.IdDetallePagoOriginal)
                .Select(g => new { Id = g.Key, Cantidad = g.Sum(d => d.Cantidad) })
                .ToDictionaryAsync(x => x.Id, x => x.Cantidad);
        }
    }
}

