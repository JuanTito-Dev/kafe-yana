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

        /// <summary>Correlativo SIAT: MAX(NumeroFactura) + 1 solo entre ventas facturadas.</summary>
        public async Task<long> SiguienteNumeroFacturaSiatAsync()
        {
            var result = await _db.Database
                .SqlQueryRaw<long>(
                    "SELECT COALESCE(MAX(\"NumeroFactura\"), 0) + 1 FROM \"Venta\" WHERE \"Facturado\" = true")
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
