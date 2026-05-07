using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class InsumoMovimientoRepositorio : GenericRepositorio<InsumoMovimiento>, IInsumoMovimientoRepositorio
    {
        public InsumoMovimientoRepositorio(AppDbContext _db) : base(_db)
        {

        }
    }
}
