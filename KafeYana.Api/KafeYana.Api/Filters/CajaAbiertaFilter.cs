using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KafeYana.Api.Filters
{
    public class CajaAbiertaFilter : IAsyncActionFilter
    {
        private readonly IUnitWork _db;

        public CajaAbiertaFilter(IUnitWork db)
        {
            _db = db;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var caja = await _db.cajas.ObtenerCaja();

            if (caja is null)
                throw new CajaException("No hay una caja abierta");

            context.HttpContext.Items["Caja"] = caja;

            await next();
        }
    }
}
