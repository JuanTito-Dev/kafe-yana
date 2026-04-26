using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class VariacionRepositorio : GenericRepositorio<Variacion>, IVariacionReposiotorio
    {
        public VariacionRepositorio(AppDbContext _db) : base(_db)
        {

        }

        public async Task CrearOpcion(Opcion opcion)
        {
           await _db.Opciones.AddAsync(opcion);
        }

        public Task DeleteOpcion(Opcion datos)
        {
            _db.Opciones.Remove(datos);  

            return Task.CompletedTask;
        }

        public async Task<bool> ExisteOpcion(int Id)
        {
            return await _db.Opciones.AnyAsync(x => x.Id == Id);
        }

        public async Task<Opcion?> TraerOpcion(int Id)
        {
            var opcion = await _db.Opciones
                .Include(x => x.Ajustes).ThenInclude(a => a.InsumoBase)
                .Include(x => x.Ajustes).ThenInclude(a => a.InsumoNuevo)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (opcion is null) return null;

            return opcion;
        }
    }
}
