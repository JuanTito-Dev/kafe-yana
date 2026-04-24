using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.IRepositorio
{
    public interface IGenericRepositorio<T> where T : BaseEntity 
    {
        Task<bool> Crear(T Modelo);
        Task GuardarAsync();
        Task<bool> Eliminar(int Id);

        Task<T> Obtener(int Id);
    }
}
