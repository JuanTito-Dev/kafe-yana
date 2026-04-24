using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class ClienteRepositorio : GenericRepositorio<Cliente>, IClienteRespositorio
    {
        public readonly DbSet<Cliente> _clientes;
        public ClienteRepositorio(AppDbContext _db) : base(_db)
        {
            _clientes = _db.Set<Cliente>();
        }

        public IQueryable<Cliente> GetClientes()
        {
            return _clientes.AsQueryable();
        }

        public async Task<Cliente?> GetCliente(int Id)
        {
            return await _clientes.FindAsync(Id);
        }
    }
}
