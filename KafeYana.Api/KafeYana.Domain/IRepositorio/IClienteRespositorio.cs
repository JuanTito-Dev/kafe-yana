using KafeYana.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IClienteRespositorio : IGenericRepositorio<Cliente>
    {
        public IQueryable<Cliente> GetClientes();

        public Task<Cliente?> GetCliente(int Id);
    }
}
