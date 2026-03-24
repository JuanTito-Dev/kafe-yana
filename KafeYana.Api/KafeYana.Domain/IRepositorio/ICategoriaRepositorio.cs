using KafeYana.Application.Dtos.Categoria;
using KafeYana.Core.Entities.Inventario;
using KafeYana.Domain.Entities.BaseEntidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface ICategoriaRepositorio : IGenericRepositorio<Categoria>
    {
        public Task<Categoria> BuscarConProductos(int Id);

        Task<List<DtoCategoriaLista>> ObtenerTodosAsync();
    }
}
