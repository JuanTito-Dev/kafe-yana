using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IGenericRepositorio<T> where T : BaseEntity
    {
        public Task<IReadOnlyList<T>> GetAll();
        public Task Crear(T datos);

        public Task<T?> FindByIdAsync(int Id);

        public Task Remove(T datos);

        public Task SaveAsync();
    }
}
