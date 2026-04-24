using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UsaAutoPartes.Application.Dtos.ImportacionDtos;
using UsaAutoPartes.Application.Dtos.ProductosDtos;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities; 
using UsaAutoPartes.Domain.Enum.UsuarioEnums;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles =  UsuarioRoles.Admin)]
    public class ProductoController(IProductoRepositorio _producto, IUnitWork _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(ProductoCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var prducto = datos.Adapt<Producto>();

            await _producto.Crear(prducto);

            await _producto.GuardarAsync();

            return Created();
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Editar(int Id, ProductoCU datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var productoBd = await _producto.Obtener(Id);

            productoBd = datos.Adapt(productoBd);

            await _producto.GuardarAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _producto.Eliminar(id);
            await _producto.GuardarAsync();
            return NoContent();
        }

        [HttpPost("lista")]
        public async Task<IActionResult> CrearLista(List<DtoProductosLista> Lista)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (Lista == null || !Lista.Any())
                return BadRequest(new { mensaje = "La lista está vacía." });


            int CreadoCant = 0;
            int ActualizadoCant = 0;

            foreach (var item in Lista)
            {
                var producto = await _db.productos.GetProductoforCodigo(item.Codigo);

                if (producto != null)
                {
                    item.Actualizar(producto);
                    ActualizadoCant++;
                }
                else
                {
                    var newproducto = item.Crear();
                    await _db.productos.Crear(newproducto);
                    CreadoCant++;

                }
            }

            try
            {
                await _db.SaveUnitWork();
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al guardar, ningún cambio fue aplicado.", detalle = ex.Message });
            }

            var res = new DtoRespuestaLista(ActualizadoCant, CreadoCant);

            return Ok(res);
        }

        [HttpPost("importacion")]
        public async Task<IActionResult> Importar(DtoImportacionLista datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var proveedor = await _db.proveedores.Obtener(datos.Id_Proveedor);

            if (proveedor is null)
            {
                return BadRequest("Proveedor no encontrado");
            }

            if (datos.Productos == null || !datos.Productos.Any())
                return BadRequest(new { mensaje = "La lista está vacía." });

            int CreadoCant = 0;
            int ActualizadoCant = 0;

            foreach (var item in datos.Productos)
            {
                var producto = await _db.productos.GetProductoforCodigo(item.Codigo);

                if (producto != null)
                {
                    item.Actualizar(producto);
                    ActualizadoCant++;
                }
                else
                {
                    var newproducto = item.Crear();
                    await _db.productos.Crear(newproducto);
                    CreadoCant++;

                }
            }

            proveedor.CanImportaciones++;
            proveedor.Total += datos.CostoTotal;

            var correlativo = await _db.importaciones.Count(x => x.Fecha.Year == datos.Fecha.Year) + 1;

            var importacion = new Importacion
            {
                Codigo = $"IMP-{datos.Fecha.Year}-{correlativo.ToString()}",
                Id_Proveedor = datos.Id_Proveedor,
                Fecha = datos.Fecha,
                Total = datos.CostoTotal,
                CantProductos = datos.Productos.Count()
            };

            await _db.importaciones.Crear(importacion);

            try
            {
                await _db.SaveUnitWork();
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al guardar, ningún cambio fue aplicado.", detalle = ex.Message });
            }

            var res = new DtoRespuestaLista(ActualizadoCant, CreadoCant);

            return Ok(res);
        }
    }
}
