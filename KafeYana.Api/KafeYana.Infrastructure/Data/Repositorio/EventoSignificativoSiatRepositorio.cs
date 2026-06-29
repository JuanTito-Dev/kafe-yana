using System;
using System.Threading;
using System.Threading.Tasks;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Facturacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class EventoSignificativoSiatRepositorio : GenericRepositorio<EventoSignificativoSiat>, IEventoSignificativoSiatRepositorio
    {
        private readonly ILogger<EventoSignificativoSiatRepositorio>? _logger;

        public EventoSignificativoSiatRepositorio(AppDbContext db) : base(db)
        {
        }

        public EventoSignificativoSiatRepositorio(
            AppDbContext db,
            ILogger<EventoSignificativoSiatRepositorio> logger) : base(db)
        {
            _logger = logger;
        }

        public async Task<EventoSignificativoSiat?> ObtenerContingenciaActivaAsync(
            int codigoSucursal,
            int codigoPuntoVenta,
            CancellationToken ct = default)
        {
            return await _Set
                .Where(e => e.EstadoContingencia == EventoContingenciaEstado.Activo
                    && e.CodigoSucursal == codigoSucursal
                    && e.CodigoPuntoVenta == codigoPuntoVenta)
                .OrderByDescending(e => e.FechaRegistro)
                .FirstOrDefaultAsync(ct);
        }

        public async Task CerrarContingenciaAsync(int eventoSignificativoId, CancellationToken ct = default)
        {
            var entity = await _Set.FindAsync(new object[] { eventoSignificativoId }, ct);
            if (entity is null) return;

            if (entity.EstadoContingencia == EventoContingenciaEstado.Cerrado)
                return;

            // No se puede "cerrar" un Rechazado desde acá: ese estado indica
            // que el SIAT rechazó el registro, NO que el operador decidió cerrar
            // la contingencia. La recuperación de un Rechazado requiere
            // intervención manual (revisar por qué fue rechazado, corregir, y
            // posiblemente abrir un evento nuevo). Ver [[kafeyana-contingencia-siat]].
            if (entity.EstadoContingencia == EventoContingenciaEstado.Rechazado)
            {
                _logger?.LogWarning(
                    "CerrarContingenciaAsync llamado sobre evento Rechazado {Id}. Ignorando.",
                    eventoSignificativoId);
                return;
            }

            entity.EstadoContingencia = EventoContingenciaEstado.Cerrado;
            entity.FechaCierre = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<EventoSignificativoSiat>> ListarContingenciasActivasAsync(
            CancellationToken ct = default)
        {
            return await _Set
                .AsNoTracking()
                // Sólo Activos: Cerrados (éxitos) y Rechazados (SIAT rechazó)
                // NO son contingencias pendientes y deben quedar fuera del
                // snapshot que hidrata el monitor al boot.
                .Where(e => e.EstadoContingencia == EventoContingenciaEstado.Activo)
                .OrderByDescending(e => e.FechaRegistro)
                .ToListAsync(ct);
        }
    }
}