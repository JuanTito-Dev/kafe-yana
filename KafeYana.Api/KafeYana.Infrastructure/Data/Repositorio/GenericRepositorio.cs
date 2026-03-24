using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.BaseEntidades;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
