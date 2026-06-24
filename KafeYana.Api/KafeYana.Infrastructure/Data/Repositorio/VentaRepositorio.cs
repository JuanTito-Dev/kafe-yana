using KafeYana.Application.Exceptions.Usuarios;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<Venta?> TraerVentaConDetallesAsync(int id)
        {
            return await _db.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == id);
        }
    }
}
