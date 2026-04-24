using KafeYana.Application.Dtos.CompradoDtos;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    public interface IProductoRepositorio : IGenericRepositorio<Producto>
    {


        public Task<IReadOnlyList<Producto>> GetProductos(string? tipo = null, string? categoria = null, string? Nombre = null);

        public IQueryable<Comprado> GetComprados();

        public Task<Producto?> GetComprado(int Id);

        public Task<Comprado?> GetCompradowithproducto(int id);

    }
}
