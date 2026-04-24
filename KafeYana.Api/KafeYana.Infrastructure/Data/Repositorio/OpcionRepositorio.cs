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
    public class OpcionRepositorio : GenericRepositorio<Opcion>, IOpcionRepositorio
    {
        public OpcionRepositorio(AppDbContext _db) : base(_db)
        {
        }

        public async Task<bool> Opciondeproducto(int Id, int Id_Opcion)
        {
            return await _db.Opciones.AnyAsync(x => x.Id == Id_Opcion && x.Variacion.Elaborado.Producto.Id == Id);
        }

        public async Task<Opcion?> TraerOpcion(int? Id)
        {
            if (Id == null) return null;
            return await _db.Opciones
                .Include(x => x.Ajustes)
                    .ThenInclude(x => x.InsumoBase)
                .Include(x => x.Ajustes)
                    .ThenInclude(x => x.InsumoNuevo)  // 👈 esto te falta
                .FirstOrDefaultAsync(x => x.Id == Id);
        }
    }
}
