using KafeYana.Application.Dtos.PromocionTemporadaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = RolesKafe.Admin)]
    public class PromocionTemporadaController(IUnitWork _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoPromocionTemporadaCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (datos.FechaFin < datos.FechaInicio)
                return BadRequest(new { message = "FechaFin debe ser mayor o igual a FechaInicio" });

            var ids = datos.IdsProductosCanjeables.Distinct().ToList();
            if (ids.Count == 0)
                return BadRequest(new { message = "Debe incluir al menos un Id en IdsProductosCanjeables" });

            if (ids.Count != datos.IdsProductosCanjeables.Count)
                return BadRequest(new { message = "No se permiten Ids duplicados en IdsProductosCanjeables" });

            await ValidarTodosProductosCanjeablesExisten(ids);

            var promo = new PromocionTemporada
            {
                Nombre       = datos.Nombre,
                FechaInicio  = datos.FechaInicio,
                FechaFin     = datos.FechaFin,
                Activo       = datos.Activo,
                ProductosCanjeables = ids.Select(pid => new PromocionTemporadaProductoCanjeable
                {
                    Id_ProductoCanjeable = pid
                }).ToList()
            };

            await _db.promocionTemporadas.Crear(promo);
            await _db.SaveUnitWork();

            return Created("", new { message = "Promoción por temporada creada", promo.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, DtoPromocionTemporadaCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (datos.FechaFin < datos.FechaInicio)
                return BadRequest(new { message = "FechaFin debe ser mayor o igual a FechaInicio" });

            var ids = datos.IdsProductosCanjeables.Distinct().ToList();
            if (ids.Count == 0)
                return BadRequest(new { message = "Debe incluir al menos un Id en IdsProductosCanjeables" });

            if (ids.Count != datos.IdsProductosCanjeables.Count)
                return BadRequest(new { message = "No se permiten Ids duplicados en IdsProductosCanjeables" });

            await ValidarTodosProductosCanjeablesExisten(ids);

            var promo = await _db.promocionTemporadas.ObtenerConEnlacesTrackedAsync(id);
            if (promo is null)
                return NotFound(new { message = "Promoción por temporada no encontrada" });

            promo.Nombre      = datos.Nombre;
            promo.FechaInicio = datos.FechaInicio;
            promo.FechaFin    = datos.FechaFin;
            promo.Activo      = datos.Activo;

            promo.ProductosCanjeables.Clear();
            foreach (var pid in ids)
            {
                promo.ProductosCanjeables.Add(new PromocionTemporadaProductoCanjeable
                {
                    Id_ProductoCanjeable = pid
                });
            }

            await _db.SaveUnitWork();

            return Ok(new { message = "Promoción por temporada actualizada" });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var promo = await _db.promocionTemporadas.FindByIdAsync(id);
            if (promo is null)
                return NotFound(new { message = "Promoción por temporada no encontrada" });

            await _db.promocionTemporadas.Remove(promo);
            await _db.SaveUnitWork();

            return Ok(new { message = "Promoción por temporada eliminada" });
        }

        private async Task ValidarTodosProductosCanjeablesExisten(List<int> ids)
        {
            foreach (var pid in ids)
            {
                if (!await _db.productosCanjeables.Existe(pid))
                    throw new InventarioException($"Producto canjeable no encontrado (Id {pid})");
            }
        }
    }
}
