using KafeYana.Application.Dtos.HitoCompraDtos;
using KafeYana.Application.Dtos.ProductoCanjeable;
using KafeYana.Application.Dtos.PromocionTemporadaDtos;
using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Entities;
using KafeYana.Domain.TiposDeDatos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KafeYana.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{RolesKafe.Admin}, {RolesKafe.Cajero}")]
    public class ProductoCanjeableController(
        IUnitWork _db,
        ICanjeProductoService _canjeProducto,
        IPromocionPermanenteProductoGratisService _productoGratis,
        IPromocionTemporadaReclamoService _promocionTemporada,
        IHitoCompraReclamoService _hitoCompra) : ControllerBase
    {
        [HttpGet("hitos-reclamados")]
        public async Task<IActionResult> ObtenerHitosReclamados([FromQuery] int Id_Cliente)
        {
            if (Id_Cliente <= 0)
                return BadRequest(new { message = "Id_Cliente es obligatorio." });

            var resultado = await _hitoCompra.ObtenerHitosReclamadosAsync(Id_Cliente);
            return Ok(resultado);
        }

        [HttpPost("reclamar-hito")]
        public async Task<IActionResult> ReclamarHito(DtoReclamarHitoCompra dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _hitoCompra.ReclamarAsync(dto);
            return Ok(resultado);
        }

        [HttpGet("promociones-temporada-disponibles")]
        public async Task<IActionResult> ObtenerPromocionesTemporadaDisponibles([FromQuery] int Id_Cliente)
        {
            if (Id_Cliente <= 0)
                return BadRequest(new { message = "Id_Cliente es obligatorio." });

            var resultado = await _promocionTemporada.ObtenerProductosReclamablesAsync(Id_Cliente);
            return Ok(resultado);
        }

        [HttpPost("reclamar-promocion-temporada")]
        public async Task<IActionResult> ReclamarPromocionTemporada(DtoReclamarPromocionTemporada dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _promocionTemporada.ReclamarAsync(dto);
            return Ok(resultado);
        }

        [HttpGet("promociones-gratis-disponibles")]
        public async Task<IActionResult> ObtenerPromocionesGratisDisponibles([FromQuery] int Id_Cliente)
        {
            if (Id_Cliente <= 0)
                return BadRequest(new { message = "Id_Cliente es obligatorio." });

            var resultado = await _productoGratis.ObtenerPromocionesGratisClienteAsync(Id_Cliente);
            return Ok(resultado);
        }

        [HttpPost("reclamar-promocion-gratis")]
        public async Task<IActionResult> ReclamarPromocionGratis(DtoReclamarPromocionGratis dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _productoGratis.ReclamarAsync(dto);
            return Ok(resultado);
        }

        [HttpPost("canje")]
        public async Task<IActionResult> EjecutarCanje(DtoCanjeProducto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _canjeProducto.EjecutarCanjeAsync(dto);

            return Ok(new { message = "Canje registrado correctamente." });
        }

        [HttpPost]
        public async Task<IActionResult> Crear(DtoProductoCanjeableCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!datos.Activo)
                return BadRequest(new { message = "No se puede crear un producto canjeable inactivo" });

            var producto = await _db.productos.Query()
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == datos.Id_Producto);

            if (producto is null)
                throw new InventarioException("Producto no encontrado");

            var canjeable = new ProductoCanjeable
            {
                Id_Producto    = datos.Id_Producto,
                NombreProducto = producto.Nombre,
                Categoria      = producto.Categoria.Nombre,
                Puntos         = datos.Puntos,
                Disponible     = datos.Disponible,
                Activo         = datos.Activo
            };

            await _db.productosCanjeables.Crear(canjeable);
            await _db.SaveUnitWork();

            return Created("", new { message = "Producto canjeable creado", canjeable.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, DtoProductoCanjeableCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var canjeable = await _db.productosCanjeables.FindByIdAsync(id);
            if (canjeable is null)
                return NotFound(new { message = "Producto canjeable no encontrado" });

            // Si cambia el producto, refrescar nombre y categoría
            if (canjeable.Id_Producto != datos.Id_Producto)
            {
                var producto = await _db.productos.Query()
                    .Include(p => p.Categoria)
                    .FirstOrDefaultAsync(p => p.Id == datos.Id_Producto);

                if (producto is null)
                    throw new InventarioException("Producto no encontrado");

                canjeable.Id_Producto    = datos.Id_Producto;
                canjeable.NombreProducto = producto.Nombre;
                canjeable.Categoria      = producto.Categoria.Nombre;
            }

            canjeable.Puntos     = datos.Puntos;
            canjeable.Disponible = datos.Disponible;
            canjeable.Activo     = datos.Activo;

            await _db.SaveUnitWork();

            return Ok(new { message = "Producto canjeable actualizado" });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var canjeable = await _db.productosCanjeables.FindByIdAsync(id);
            if (canjeable is null)
                return NotFound(new { message = "Producto canjeable no encontrado" });

            await _db.productosCanjeables.Remove(canjeable);
            await _db.SaveUnitWork();

            return Ok(new { message = "Producto canjeable eliminado" });
        }
    }
}
