using KafeYana.Application.Dtos.PromocionPermanenteDtos;
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
    public class PromocionPermanenteController(IUnitWork _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoPromocionPermanenteCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (datos.TipoRecompensa == TipoRecompensaPromocion.ProductoGratis && datos.Id_ProductoCanjeable is null)
                return BadRequest(new { message = "Id_ProductoCanjeable es obligatorio cuando la recompensa es ProductoGratis" });

            if (datos.TipoRecompensa != TipoRecompensaPromocion.ProductoGratis && datos.Id_ProductoCanjeable is not null)
                return BadRequest(new { message = "Id_ProductoCanjeable solo aplica cuando la recompensa es ProductoGratis" });

            if (datos.TipoRecompensa != TipoRecompensaPromocion.ProductoGratis && datos.ValorRecompensa <= 0)
                return BadRequest(new { message = "ValorRecompensa debe ser mayor a 0" });

            var promocion = new PromocionPermanente
            {
                Nombre               = datos.Nombre,
                Descripcion          = datos.Descripcion,
                TipoCondicion        = datos.TipoCondicion,
                ValorCondicion       = datos.ValorCondicion,
                TipoRecompensa       = datos.TipoRecompensa,
                ValorRecompensa      = datos.ValorRecompensa,
                Activo               = datos.Activo,
                Id_ProductoCanjeable = datos.Id_ProductoCanjeable
            };

            await _db.promocionPermanentes.Crear(promocion);
            await _db.SaveUnitWork();

            return Created("", new { message = "Promoción permanente creada", promocion.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, DtoPromocionPermanenteCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (datos.TipoRecompensa == TipoRecompensaPromocion.ProductoGratis && datos.Id_ProductoCanjeable is null)
                return BadRequest(new { message = "Id_ProductoCanjeable es obligatorio cuando la recompensa es ProductoGratis" });

            if (datos.TipoRecompensa != TipoRecompensaPromocion.ProductoGratis && datos.Id_ProductoCanjeable is not null)
                return BadRequest(new { message = "Id_ProductoCanjeable solo aplica cuando la recompensa es ProductoGratis" });

            if (datos.TipoRecompensa != TipoRecompensaPromocion.ProductoGratis && datos.ValorRecompensa <= 0)
                return BadRequest(new { message = "ValorRecompensa debe ser mayor a 0" });

            var promocion = await _db.promocionPermanentes.FindByIdAsync(id);
            if (promocion is null)
                return NotFound(new { message = "Promoción permanente no encontrada" });

            promocion.Nombre               = datos.Nombre;
            promocion.Descripcion          = datos.Descripcion;
            promocion.TipoCondicion        = datos.TipoCondicion;
            promocion.ValorCondicion       = datos.ValorCondicion;
            promocion.TipoRecompensa       = datos.TipoRecompensa;
            promocion.ValorRecompensa      = datos.ValorRecompensa;
            promocion.Activo               = datos.Activo;
            promocion.Id_ProductoCanjeable = datos.Id_ProductoCanjeable;

            await _db.SaveUnitWork();

            return Ok(new { message = "Promoción permanente actualizada" });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var promocion = await _db.promocionPermanentes.FindByIdAsync(id);
            if (promocion is null)
                return NotFound(new { message = "Promoción permanente no encontrada" });

            await _db.promocionPermanentes.Remove(promocion);
            await _db.SaveUnitWork();

            return Ok(new { message = "Promoción permanente eliminada" });
        }
    }
}
