using HotChocolate.Authorization;
using KafeYana.Application.Auxiliares.Recetas;
using KafeYana.Application.Dtos.CompradoDtos;
using KafeYana.Application.Dtos.ElaboradosDtos;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace KafeYana.Api.GraphQLMap
{
    [ExtendObjectType("Query")]
    public class ElaboradoQuery
    {
        [Authorize(Roles = new[] { "Admin" })]
        [UsePaging]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public  IQueryable<Elaborado> elaborados([Service] IElaboradoRepositorio _db)
        {
            return _db.QueryElaborados(); 
            
        }

        //[Authorize(Roles = new[] { "Admin" })] 
        //public IQueryable<Elaborado?> elaborado([Service] IElaboradoRepositorio _db, int Id)
        //{
        //    if (Id <= 0) return null;

        //    return _db.QueryElaborado(Id);
        //}
    }
}
