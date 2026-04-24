using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class GenericRepositorio<T>(AppDbContext _db) : IGenericRepositorio<T> where T : BaseEntity
    {
        protected readonly AppDbContext _db = _db;
        protected readonly DbSet<T> _Set = _db.Set<T>();

        public async Task<T?> FindByIdAsync(int Id)
        {
            var entitie = await _Set.FindAsync(Id);

            if (entitie is null) return null;

            return entitie;
        }

        public async Task Crear(T datos)
        {
            await _Set.AddAsync(datos);
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public Task Remove(T datos)
        {
            _Set.Remove(datos);

            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<T>> GetAll()
        {
            return await _Set.OrderBy(x => x.Id).ToListAsync(); ;
        }

        public async Task<Producto?> TraerProducto(int Id, bool comprado = false, bool elaborado = false, bool combo = false)
        {
            var query = _db.Productos.AsQueryable();

            query = (comprado, elaborado, combo) switch
            {
                (true, false, false) => query.Where(x => x.Tipo == TiposProductos.Comprado).Include(x => x.Comprado),
                (false, true, false) => query.Where(x => x.Tipo == TiposProductos.Elaborado).Include(x => x.Elaborado),
                (false, false, true) => query.Where(x => x.Tipo == TiposProductos.Promocion).Include(x => x.Promocion).ThenInclude(x => x.Detalles),
                _ => query

            };

            return await query.FirstOrDefaultAsync(x => x.Id == Id);
        }

        public async Task<bool> Existe(int Id)
        {
            return await _Set.AnyAsync(x => x.Id == Id);
        }

        public async Task<bool> ExisteAsync(Expression<Func<T, bool>> funcion)
        {
            return await _Set.AnyAsync(funcion);
        }

        public IQueryable<T> Query()
        {
            return _Set.AsNoTracking();
        }
    }
}
