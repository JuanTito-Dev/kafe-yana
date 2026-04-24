using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class AjusteStockRepositorio : GenericRepositorio<Stock_Ajuste>, IAjusteStockRepositorio
    {
        public AjusteStockRepositorio(AppDbContext _db) : base(_db)
        {

        }

        public IQueryable<Stock_Ajuste> Stock_AjusteQuery()
        {
            return _db.AjustesStock.AsNoTracking().AsQueryable();
        }
    }
}
