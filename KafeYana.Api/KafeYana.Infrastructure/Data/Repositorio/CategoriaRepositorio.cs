using KafeYana.Application.Dtos.Categoria;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Core.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class CategoriaRepositorio : GenericRepositorio<Categoria>, ICategoriaRepositorio
    {
        //private readonly AppDbContext _db;

        public CategoriaRepositorio(AppDbContext db) : base(db)
        {
            //_db = db;
        }
        public async Task<Categoria> BuscarConProductos(int Id)
        {
            var categoria = await _db.Categorias.Include(x => x.Productos)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (categoria is null)
                throw new InventarioException($"No se encontró la categoría con id {Id}");

            return categoria;
        }

        public async Task<List<DtoCategoriaLista>> ObtenerTodosAsync()
        {
            return await _db.Categorias
                .Select(c => new DtoCategoriaLista
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Estado = c.Estado,
                    Color = c.Color,
                    Cantidad = c.Productos.Count
                })
                .ToListAsync();
        }
    }
}
