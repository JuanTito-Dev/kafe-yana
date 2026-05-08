using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class Detalle_RondaRepositorio : GenericRepositorio<Detalle_ronda>, IDetalle_RondaRepositorio
    {
        public Detalle_RondaRepositorio(AppDbContext db) : base(db)
        {
        }
    }
}