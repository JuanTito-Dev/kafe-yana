using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
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
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<IReadOnlyList<NotaAjuste>> ListarPorVentaAsync(int ventaId)
        {
            return await _db.NotasAjuste
                .Where(n => n.IdVenta == ventaId)
                .OrderByDescending(n => n.FechaEmision)
                .ToListAsync();
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
