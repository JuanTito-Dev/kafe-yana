using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Facturacion;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Infrastructure.Data.Repositorio
{
    public class CodigoSiatRepositorio : GenericRepositorio<CodigoSiat>, ICodigoSiatRepositorio
    {
        public CodigoSiatRepositorio(AppDbContext db) : base(db)
        {
        }

        public IQueryable<CodigoSiat> Buscar(
            string? codigoProducto = null,
            string? codigoActividad = null,
            string? descripcionProducto = null,
            string? descripcionActividad = null,
            string? termino = null)
        {
            var query = Query();

            if (!string.IsNullOrWhiteSpace(codigoProducto))
            {
                query = query.Where(x =>
                    EF.Functions.ILike(x.CodigoProducto, $"%{codigoProducto.Trim()}%"));
            }

            if (!string.IsNullOrWhiteSpace(codigoActividad))
            {
                query = query.Where(x =>
                    EF.Functions.ILike(x.CodigoActividad, $"%{codigoActividad.Trim()}%"));
            }

            if (!string.IsNullOrWhiteSpace(descripcionProducto))
            {
                query = query.Where(x =>
                    EF.Functions.ILike(x.DescripcionProducto, $"%{descripcionProducto.Trim()}%"));
            }

            if (!string.IsNullOrWhiteSpace(descripcionActividad))
            {
                query = query.Where(x =>
                    EF.Functions.ILike(x.DescripcionActividad, $"%{descripcionActividad.Trim()}%"));
            }

            if (!string.IsNullOrWhiteSpace(termino))
            {
                var t = termino.Trim();
                query = query.Where(x =>
                    EF.Functions.ILike(x.CodigoProducto, $"%{t}%")
                    || EF.Functions.ILike(x.CodigoActividad, $"%{t}%")
                    || EF.Functions.ILike(x.DescripcionProducto, $"%{t}%")
                    || EF.Functions.ILike(x.DescripcionActividad, $"%{t}%"));
            }

            return query.OrderBy(x => x.CodigoProducto).ThenBy(x => x.CodigoActividad);
        }
    }
}
