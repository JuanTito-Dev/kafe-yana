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
    }
}
