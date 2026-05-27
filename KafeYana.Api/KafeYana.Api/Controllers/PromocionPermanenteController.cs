using KafeYana.Application.Dtos.PromocionPermanenteDtos;

using KafeYana.Application.Exceptions;

using KafeYana.Application.IRepositorio;

using KafeYana.Application.IServicios;

using KafeYana.Domain.Entities;

using KafeYana.Domain.TiposDeDatos;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;



namespace KafeYana.Api.Controllers

{

    [Route("api/[controller]")]

    [ApiController]

    public class PromocionPermanenteController(

        IUnitWork _db,

        IPromocionPermanenteDescuentoService _descuentoService) : ControllerBase

    {

        /// <summary>Preview de descuentos permanentes aplicables al pedido (sin persistir).</summary>

        [HttpGet("descuentos-pedido")]

        [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}, {RolesKafe.Mesero}")]

        public async Task<IActionResult> ObtenerDescuentosPedido(

            [FromQuery] int Id_Pedido,

            [FromQuery] int Id_Cliente)

        {

            if (Id_Pedido <= 0 || Id_Cliente <= 0)

                return BadRequest(new { message = "Id_Pedido e Id_Cliente son obligatorios." });



            var resultado = await _descuentoService.ObtenerDescuentosPedidoAsync(Id_Pedido, Id_Cliente);

            return Ok(resultado);

        }



        [HttpPost]

        [Authorize(Roles = RolesKafe.Admin)]

        public async Task<IActionResult> Crear(DtoPromocionPermanenteCU datos)

        {

            if (!ModelState.IsValid) return BadRequest(ModelState);



            var errorValidacion = ValidarDto(datos);

            if (errorValidacion is not null)

                return BadRequest(new { message = errorValidacion });



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

        [Authorize(Roles = RolesKafe.Admin)]

        public async Task<IActionResult> Actualizar(int id, DtoPromocionPermanenteCU datos)

        {

            if (!ModelState.IsValid) return BadRequest(ModelState);



            var errorValidacion = ValidarDto(datos);

            if (errorValidacion is not null)

                return BadRequest(new { message = errorValidacion });



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

        [Authorize(Roles = RolesKafe.Admin)]

        public async Task<IActionResult> Eliminar(int id)

        {

            var promocion = await _db.promocionPermanentes.FindByIdAsync(id);

            if (promocion is null)

                return NotFound(new { message = "Promoción permanente no encontrada" });



            await _db.promocionPermanentes.Remove(promocion);

            await _db.SaveUnitWork();



            return Ok(new { message = "Promoción permanente eliminada" });

        }



        private static string? ValidarDto(DtoPromocionPermanenteCU datos)

        {

            if (datos.TipoRecompensa == TipoRecompensaPromocion.ProductoGratis && datos.Id_ProductoCanjeable is null)

                return "Id_ProductoCanjeable es obligatorio cuando la recompensa es ProductoGratis";



            if (datos.TipoRecompensa != TipoRecompensaPromocion.ProductoGratis && datos.Id_ProductoCanjeable is not null)

                return "Id_ProductoCanjeable solo aplica cuando la recompensa es ProductoGratis";



            if (datos.TipoRecompensa != TipoRecompensaPromocion.ProductoGratis && datos.ValorRecompensa <= 0)

                return "ValorRecompensa debe ser mayor a 0";



            if (datos.TipoRecompensa == TipoRecompensaPromocion.Descuento && datos.ValorRecompensa > 100)

                return "ValorRecompensa para Descuento debe ser un porcentaje entre 1 y 100";



            return null;

        }

    }

}


