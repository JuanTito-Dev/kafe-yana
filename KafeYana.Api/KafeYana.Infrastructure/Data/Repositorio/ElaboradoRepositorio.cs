using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class ElaboradoRepositorio : GenericRepositorio<Producto> , IElaboradoRepositorio
    {
        private readonly DbSet<Elaborado> Elaborado;
        public ElaboradoRepositorio(AppDbContext _db) : base (_db)
        {
            Elaborado = _db.Set<Elaborado>();
        }

        public async Task<Elaborado?> ElaboradoWithProducto(int id)
        {
            return await Elaborado.Include(x => x.Producto).FirstOrDefaultAsync(x => x.Id_Producto == id);
        }

        public IQueryable<Elaborado> QueryElaborados()
        {
            return Elaborado.AsNoTracking().AsSplitQuery().AsQueryable();
        }

        public IQueryable<Elaborado> QueryElaborado(int Id)
        {
            return Elaborado.Include(x => x.Producto).Where(x => x.Producto.Id == Id && x.Producto.Tipo == TiposProductos.Elaborado).AsQueryable();
        }

        public async Task<Elaborado?> TraerElaborado(int Id, bool withreceta = false)
        {
            var query = Elaborado.Include(x => x.Producto).AsQueryable();

            if (withreceta)
            {
                query = query.Include(x => x.Receta).ThenInclude(x => x.Detalles).ThenInclude(x => x.Insumo);
            }

            return await query.FirstOrDefaultAsync(x => x.Producto.Id == Id && x.Producto.Tipo == TiposProductos.Elaborado);
        }

        public async Task<bool> EsProducible(int Id)
        {
            return await Elaborado.AnyAsync(x => x.Id_Producto == Id && x.Producible == true);
        }
    }
}
