using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class CajaHistorialMovimientoRepositorio : GenericRepositorio<CajaHistorialMovimiento>, ICajaHistorialMovimientoRepositorio
    {
        public CajaHistorialMovimientoRepositorio(AppDbContext _db) : base(_db)
        {
        }
    }
}
