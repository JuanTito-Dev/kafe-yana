using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Domain.Dtos.Detalle_RondaDtos;
using KafeYana.Domain.Dtos.RondaDtos;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KafeYana.Infrastructure.Servicios
{
    public class Detalle_RondaService
    {
        private readonly IUnitWork _unitWork;

        public Detalle_RondaService(IUnitWork unitWork, AppDbContext db)
        {
            _unitWork = unitWork;
        }

        public async Task<Ronda> CrearRondaConDetallesAsync(int idPedido, List<DtoRondadetalle> detallesDto)
        {
            var listaDetalles = new List<Detalle_ronda>();
            var subtotal = 0.00M;
            var numeroRonda = await _unitWork.rondas.Count(x => x.Id_Pedido == idPedido) + 1;
            var referencia = $"PED-{numeroRonda}-{DateTime.UtcNow:yyyyMMdd}";



            foreach (var item in detallesDto)
            {

                var tipo = await _unitWork.productos.TraerTipoProducto(item.Id_Producto);

                if (tipo is null)
                    throw new DetalleRondaException($"El producto con ID {item.Id_Producto} no existe.");

                Detalle_ronda detalle = tipo switch
                {
                    TiposProductos.Comprado => await ProcesarComprado(item.Id_Producto, item.Cantidad, referencia),
                    TiposProductos.Elaborado => await ProcesarElaborado(item.Id_Producto, item.Cantidad, item.Ids_Opcion ?? new List<int>(), referencia),
                    TiposProductos.Promocion => await ProcesarCombo(item.Id_Producto, item.Cantidad, referencia),

                    _ => throw new DetalleRondaException($"Tipo de producto desconocido: {tipo}")
                };

                detalle.Id_Producto = item.Id_Producto;
                detalle.Nota = item.Nota ?? string.Empty;

                subtotal += detalle.Precio * detalle.Cantidad;

                listaDetalles.Add(detalle);
            }

            if (listaDetalles.Count == 0)
                throw new DetalleRondaException("No se han agregado productos a la ronda.");

           

            var ronda = new Ronda
            {
                Id_Pedido = idPedido,
                Ronda_Descripcion = $"Ronda {numeroRonda}",
                Detalle = listaDetalles,
                SubTotal = subtotal
            };

            return ronda;
        }

        private async Task<Detalle_ronda> ProcesarComprado(int idProducto, int cantidad, string referencia)
        {
            var producto = await _unitWork.productos.TraerProducto(idProducto, comprado: true);

            if (producto?.Comprado is null)
                throw new DetalleRondaException($"Producto comprado no encontrado: {idProducto}");

            if (producto.Comprado.Stock_actual < cantidad)
                throw new DetalleRondaException($"Stock insuficiente para {producto.Nombre}. Disponible: {producto.Comprado.Stock_actual}, Solicitado: {cantidad}");

            var movimiento = producto.Comprado.Venta(cantidad, referencia);
            await _unitWork.movimientos.Crear(movimiento);

            return new Detalle_ronda
            {
                Nombre_Producto = producto.Nombre,
                Cantidad        = cantidad,
                Precio          = producto.Precio,
                Ubicacion       = producto.Comprado?.Ubicacion ?? string.Empty
            };
        }

        private async Task<Detalle_ronda> ProcesarElaborado(int idProducto, int cantidad, List<int> idsOpciones, string referencia)
        {
            var elaborado = await _unitWork.elaborados.TraerElaborado(idProducto, withreceta: true);

            if (elaborado is null)
                throw new DetalleRondaException($"Elaborado no encontrado: {idProducto}");

            if (elaborado.Producible)
            {
                if (elaborado.Stock_actual < cantidad)
                    throw new DetalleRondaException($"Stock insuficiente para {elaborado.Producto.Nombre}. Disponible: {elaborado.Stock_actual}, Solicitado: {cantidad}");

                var movimiento = elaborado.Venta(cantidad, referencia, 0.00M);
                await _unitWork.movimientos.Crear(movimiento);

                return new Detalle_ronda
                {
                    Nombre_Producto = elaborado.Producto.Nombre,
                    Cantidad        = cantidad,
                    Precio          = elaborado.Producto.Precio,
                    Ubicacion       = elaborado.Ubicacion ?? string.Empty
                };
            }

            return await ProcesarNoProducible(idProducto, cantidad, idsOpciones, referencia, elaborado);
        }

        private async Task<Detalle_ronda> ProcesarNoProducible(int idProducto, int cantidad, List<int> idsOpciones, string referencia, Elaborado elaborado)
        {
            var precioAjuste = 0.00M;
            var nombresOpciones = new List<string>();
            var opcionesEntity = new List<Opcion>();

            if (idsOpciones.Count > 0)
            {
                // Validar que todas las opciones pertenecen al producto
                foreach (var idOpcion in idsOpciones)
                {
                    var valida = await _unitWork.opciones.Opciondeproducto(idProducto, idOpcion);
                    if (!valida)
                        throw new DetalleRondaException($"La opción {idOpcion} no pertenece al producto {idProducto}.");
                }

                opcionesEntity = await _unitWork.opciones.GetOpcionesByIds(idsOpciones);

                foreach (var opcion in opcionesEntity)
                {
                    precioAjuste += opcion.AjustePrecio;
                    nombresOpciones.Add(opcion.Nombre);
                }
            }

            // Si no tiene receta solo guardamos
            if (elaborado.Receta is null)
            {
                return new Detalle_ronda
                {
                    Nombre_Producto = elaborado.Producto.Nombre,
                    Cantidad = cantidad,
                    Precio = elaborado.Producto.Precio + precioAjuste,
                    Ubicacion = elaborado.Ubicacion ?? string.Empty
                };
            }

            // Insumos a omitir por reemplazo
            var insumosOmitidos = opcionesEntity
                .SelectMany(x => x.Ajustes)
                .Where(x => x.TipoAjuste == TiposAjuste.Reemplazo)
                .Select(x => x.Id_Insumo)
                .ToHashSet();

            var costo = 0.00M;

            // Descontar insumos base
            foreach (var detalleReceta in elaborado.Receta.Detalles)
            {
                if (insumosOmitidos.Contains(detalleReceta.Id_insumo))
                    continue;

                var cantidadPorPorcion = detalleReceta.Cantidad / elaborado.Receta.Porciones;
                var cantidadFinal = cantidadPorPorcion * cantidad * (1 + detalleReceta.Merma / 100);

                // Aplicar modificaciones: si existe, reemplaza el total base en vez de sumar encima
                var totalModificaciones = opcionesEntity
                    .SelectMany(x => x.Ajustes)
                    .Where(x => x.Id_Insumo == detalleReceta.Id_insumo && x.TipoAjuste == TiposAjuste.Modificacion)
                    .Sum(x => x.Cantidad);

                if (totalModificaciones > 0)
                    cantidadFinal = (totalModificaciones / elaborado.Receta.Porciones) * cantidad * (1 + detalleReceta.Merma / 100);

                if (detalleReceta.Insumo.Stock_actual < (int)cantidadFinal)
                    throw new DetalleRondaException($"Stock insuficiente para insumo {detalleReceta.Insumo.Nombre}. Disponible: {detalleReceta.Insumo.Stock_actual}, Solicitado: {(int)cantidadFinal}");

                var factor = detalleReceta.Insumo.Factor_conversion > 0 ? detalleReceta.Insumo.Factor_conversion : 1;
                costo += (cantidadFinal * detalleReceta.Insumo.Costo) / factor;

                var movimiento = detalleReceta.Insumo.AjusteVenta(referencia, (int)cantidadFinal);
                await _unitWork.Insumomovientos.Crear(movimiento);
            }

            // Descontar insumos de reemplazos
            var reemplazos = opcionesEntity
                .SelectMany(x => x.Ajustes)
                .Where(x => x.TipoAjuste == TiposAjuste.Reemplazo)
                .GroupBy(x => x.Id_InsumoNuevo)
                .ToList();

            foreach (var grupo in reemplazos)
            {
                var cantidadReemplazo = (grupo.Sum(x => x.Cantidad) / elaborado.Receta.Porciones) * cantidad;
                var insumoNuevo = grupo.First().InsumoNuevo;

                if (insumoNuevo.Stock_actual < (int)cantidadReemplazo)
                    throw new DetalleRondaException($"Stock insuficiente para insumo {insumoNuevo.Nombre}. Disponible: {insumoNuevo.Stock_actual}, Solicitado: {(int)cantidadReemplazo}");

                var factorNuevo = insumoNuevo.Factor_conversion > 0 ? insumoNuevo.Factor_conversion : 1;
                costo += (cantidadReemplazo * insumoNuevo.Costo) / factorNuevo;

                var movimiento = insumoNuevo.AjusteVenta(referencia, (int)cantidadReemplazo);
                await _unitWork.Insumomovientos.Crear(movimiento);
            }

            var nombre = nombresOpciones.Count > 0
                ? $"{elaborado.Producto.Nombre} ({string.Join(", ", nombresOpciones)})"
                : elaborado.Producto.Nombre;

            var movimientoElaborado = elaborado.Venta(cantidad, referencia, costo);
            await _unitWork.movimientos.Crear(movimientoElaborado);

            return new Detalle_ronda
            {
                Nombre_Producto = nombre,
                Cantidad        = cantidad,
                Precio          = elaborado.Producto.Precio + precioAjuste,
                Ubicacion       = elaborado.Ubicacion ?? string.Empty,
                Opciones        = opcionesEntity.Select(x => new Detalle_Ronda_Opcion
                {
                    Id_Opcion = x.Id,
                    Opcion    = x
                }).ToList()
            };
        }

        private async Task<Detalle_ronda> ProcesarCombo(int idProducto, int cantidad, string referencia)
        {
            var combo = await _unitWork.Combo.TraerPromocionCompleta(idProducto);

            if (combo is null)
                throw new DetalleRondaException($"Combo no encontrado: {idProducto}");

            if (combo.Producto is null)
                throw new DetalleRondaException($"Producto del combo no encontrado: {idProducto}");

            foreach (var detalle in combo.Detalles)
            {
                if (detalle.Producto is null)
                    throw new DetalleRondaException($"Producto no encontrado en combo {idProducto}");

                var cantidadTotal = detalle.Cantidad * cantidad;
                var referenciaCombo = $"{referencia}-{detalle.Producto.Nombre}";

                switch (detalle.Producto.Tipo)
                {
                    case TiposProductos.Comprado:
                        var comprado = detalle.Producto.Comprado;
                        if (comprado is null)
                            throw new DetalleRondaException($"Producto comprado no encontrado en combo: {detalle.Id_Producto}");

                        if (comprado.Stock_actual < cantidadTotal)
                            throw new DetalleRondaException($"Stock insuficiente para {detalle.Producto.Nombre}. Disponible: {comprado.Stock_actual}, Solicitado: {cantidadTotal}");

                        var movimientoComprado = comprado.Venta(cantidadTotal, referenciaCombo);
                        await _unitWork.movimientos.Crear(movimientoComprado);
                        break;

                    case TiposProductos.Elaborado:
                        var elaborado = detalle.Producto.Elaborado;
                        if (elaborado is null)
                            throw new DetalleRondaException($"Elaborado no encontrado en combo: {detalle.Id_Producto}");

                        if (elaborado.Producible)
                        {
                            if (elaborado.Stock_actual < cantidadTotal)
                                throw new DetalleRondaException($"Stock insuficiente para {detalle.Producto.Nombre}. Disponible: {elaborado.Stock_actual}, Solicitado: {cantidadTotal}");

                            var movimientoElaborado = elaborado.Venta(cantidadTotal, referenciaCombo, 0.00M);
                            await _unitWork.movimientos.Crear(movimientoElaborado);
                        }
                        else
                        {
                            if (elaborado.Receta is not null)
                            {
                                foreach (var detalleReceta in elaborado.Receta.Detalles)
                                {
                                    var cantidadPorPorcion = detalleReceta.Cantidad / elaborado.Receta.Porciones;
                                    var cantidadFinal = cantidadPorPorcion * cantidadTotal * (1 + detalleReceta.Merma / 100);

                                    if (detalleReceta.Insumo.Stock_actual < (int)cantidadFinal)
                                        throw new DetalleRondaException($"Stock insuficiente para insumo {detalleReceta.Insumo.Nombre}. Disponible: {detalleReceta.Insumo.Stock_actual}, Solicitado: {(int)cantidadFinal}");

                                    var movimientoInsumo = detalleReceta.Insumo.AjusteVenta(referenciaCombo, (int)cantidadFinal);
                                    await _unitWork.Insumomovientos.Crear(movimientoInsumo);
                                }
                            }
                        }
                        break;
                }
            }

            var movimiento = combo.Venta(cantidad, referencia);
            await _unitWork.movimientos.Crear(movimiento);

            var itemsCombo = combo.Detalles
                .Where(d => d.Producto is not null)
                .Select(d => new Detalle_Ronda_ComboItem
                {
                    Id_Producto = d.Producto!.Id,
                    Nombre = d.Producto.Nombre,
                    Cantidad = d.Cantidad * cantidad,
                    Ubicacion = UbicacionDeProducto(d.Producto),
                })
                .ToList();

            return new Detalle_ronda
            {
                Nombre_Producto = combo.Producto.Nombre,
                Cantidad        = cantidad,
                Precio          = combo.Producto.Precio,
                Ubicacion       = string.Empty, // los ItemsCombo tienen su propia ubicación
                ItemsCombo      = itemsCombo,
            };
        }

        private static string UbicacionDeProducto(Producto producto)
        {
            return producto.Tipo switch
            {
                TiposProductos.Comprado => producto.Comprado?.Ubicacion ?? string.Empty,
                TiposProductos.Elaborado => producto.Elaborado?.Ubicacion ?? string.Empty,
                _ => string.Empty,
            };
        }
    }
}