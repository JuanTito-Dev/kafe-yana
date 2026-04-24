using KafeYana.Domain.Entities.BaseEntidades;
using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IGenericRepositorio<T> where T : BaseEntity
    {
        public Task<bool> Existe(int Id);
        public Task<IReadOnlyList<T>> GetAll();
        public Task Crear(T datos);

        public Task<bool> ExisteAsync(Expression<Func<T, bool>> funcion);

        public Task<T?> FindByIdAsync(int Id);

        public Task Remove(T datos);

        public Task<Producto?> TraerProducto(int Id, bool comprado = false, bool elaborado = false, bool combo = false);

        public Task SaveAsync();

        IQueryable<T> Query();
    }
}
