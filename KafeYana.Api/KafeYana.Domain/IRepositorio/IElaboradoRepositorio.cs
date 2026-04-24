using KafeYana.Domain.Entities.Inventario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IElaboradoRepositorio : IGenericRepositorio<Producto>
    {
        Task<Elaborado?> ElaboradoWithProducto(int id);
        IQueryable<Elaborado> QueryElaborados();

        IQueryable<Elaborado> QueryElaborado(int Id);

        Task<Elaborado?> TraerElaborado(int Id, bool withreceta = false);

        Task<bool> EsProducible(int Id);
    }
}
