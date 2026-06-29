using KafeYana.Application.Exceptions;
using KafeYana.Application.IRepositorio;
using KafeYana.Application.IServicios;
using KafeYana.Domain.Dtos.InventarioPedido;
using KafeYana.Domain.Dtos.RondaDtos;
using KafeYana.Domain.Entities;
using KafeYana.Domain.Entities.Inventario;
using KafeYana.Domain.TiposDeDatos;
using KafeYana.Infrastructure.Servicios.Facturacion;

namespace KafeYana.Infrastructure.Servicios;

public sealed class Detalle_RondaService(
    IUnitWork _unitWork,
    IInventarioPedidoCompromisoService _compromisoService)
{
    private static readonly int UnidadMedidaPorDefecto =
        UnidadMedidaSiatCatalogo.TryGetCodigo("UNIDAD", out var codigoUnidad)
            ? codigoUnidad
            : 57;

    private static readonly int UnidadMedidaCombo =
        UnidadMedidaSiatCatalogo.TryGetCodigo("UNIDAD", out var codigoUnidadCombo)
            ? codigoUnidadCombo
            : 57;
    public async Task<Ronda> CrearRondaConDetallesAsync(int idPedido, List<DtoRondadetalle> detallesDto)
    {
        var listaDetalles = new List<Detalle_ronda>();
        var subtotal = 0.00M;
        var numeroRonda = await _unitWork.rondas.Count(x => x.Id_Pedido == idPedido) + 1;
        var referencia = GenerarReferencia(idPedido, numeroRonda);

        foreach (var item in detallesDto)
        {
            var detalle = await ProcesarDetalleAsync(idPedido, item, referencia);
            subtotal += detalle.Precio * detalle.Cantidad;
            listaDetalles.Add(detalle);
        }

        if (listaDetalles.Count == 0)
            throw new DetalleRondaException("No se han agregado productos a la ronda.");

        return new Ronda
        {
            Id_Pedido = idPedido,
            Ronda_Descripcion = $"Ronda {numeroRonda}",
            Detalle = listaDetalles,
            SubTotal = subtotal
        };
    }

    public async Task<Detalle_ronda> ProcesarDetalleAsync(int idPedido, DtoRondadetalle item, string referencia)
    {
        var tipo = await _unitWork.productos.TraerTipoProducto(item.Id_Producto);

        if (tipo is null)
            throw new DetalleRondaException($"El producto con ID {item.Id_Producto} no existe.");

        var lineas = new List<CompromisoLineaCalculo>();

        Detalle_ronda detalle = tipo switch
        {
            TiposProductos.Comprado => await ProcesarComprado(item.Id_Producto, item.Cantidad, referencia, lineas),
            TiposProductos.Elaborado => await ProcesarElaborado(
                item.Id_Producto,
                item.Cantidad,
                item.Ids_Opcion ?? [],
                referencia,
                lineas),
            TiposProductos.Promocion => await ProcesarCombo(item.Id_Producto, item.Cantidad, referencia, lineas),
            _ => throw new DetalleRondaException($"Tipo de producto desconocido: {tipo}")
        };

        detalle.Id_Producto = item.Id_Producto;
        detalle.Nota = item.Nota ?? string.Empty;
        detalle.CompromisoInventario = _compromisoService.CrearCompromiso(idPedido, referencia, lineas);

        return detalle;
    }

    public static string GenerarReferencia(int idPedido, int numeroRonda) =>
        $"PED-{idPedido}-R{numeroRonda}-{DateTime.UtcNow:yyyyMMdd}";

    public static string GenerarReferenciaEdicion(int idPedido, int idRonda) =>
        $"PED-{idPedido}-R{idRonda}-ED-{DateTime.UtcNow:yyyyMMddHHmmss}";

    private async Task<Detalle_ronda> ProcesarComprado(
        int idProducto,
        int cantidad,
        string referencia,
        List<CompromisoLineaCalculo> lineas)
    {
        var producto = await _unitWork.productos.TraerProducto(idProducto, comprado: true);

        if (producto?.Comprado is null)
            throw new DetalleRondaException($"Producto comprado no encontrado: {idProducto}");

        if (producto.Comprado.Stock_actual < cantidad)
            throw new DetalleRondaException(
                $"Stock insuficiente para {producto.Nombre}. Disponible: {producto.Comprado.Stock_actual}, Solicitado: {cantidad}");

        producto.Comprado.ComprometerStock(cantidad);

        lineas.Add(new CompromisoLineaCalculo
        {
            TipoEntidad = TiposCompromisoInventario.Comprado,
            Id_Producto = idProducto,
            Cantidad = cantidad,
            Referencia = referencia
        });

        return CrearDetalleRonda(producto, cantidad, producto.Precio, producto.Comprado.Ubicacion,
            ObtenerCodigoUnidadMedida(producto.Comprado));
    }

    private async Task<Detalle_ronda> ProcesarElaborado(
        int idProducto,
        int cantidad,
        List<int> idsOpciones,
        string referencia,
        List<CompromisoLineaCalculo> lineas)
    {
        var elaborado = await _unitWork.elaborados.TraerElaborado(idProducto, withreceta: true);

        if (elaborado is null)
            throw new DetalleRondaException($"Elaborado no encontrado: {idProducto}");

        if (elaborado.Producible)
        {
            if (elaborado.Stock_actual < cantidad)
                throw new DetalleRondaException(
                    $"Stock insuficiente para {elaborado.Producto.Nombre}. Disponible: {elaborado.Stock_actual}, Solicitado: {cantidad}");

            elaborado.ComprometerStock(cantidad);

            lineas.Add(new CompromisoLineaCalculo
            {
                TipoEntidad = TiposCompromisoInventario.Elaborado,
                Id_Producto = idProducto,
                Cantidad = cantidad,
                Referencia = referencia
            });

            return CrearDetalleRonda(elaborado.Producto, cantidad, elaborado.Producto.Precio, elaborado.Ubicacion,
                ObtenerCodigoUnidadMedida(elaborado));
        }

        return await ProcesarNoProducible(idProducto, cantidad, idsOpciones, referencia, elaborado, lineas);
    }

    private async Task<Detalle_ronda> ProcesarNoProducible(
        int idProducto,
        int cantidad,
        List<int> idsOpciones,
        string referencia,
        Elaborado elaborado,
        List<CompromisoLineaCalculo> lineas)
    {
        var precioAjuste = 0.00M;
        var nombresOpciones = new List<string>();
        var opcionesEntity = new List<Opcion>();

        if (idsOpciones.Count > 0)
        {
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

        if (elaborado.Receta is null)
        {
            return CrearDetalleRonda(elaborado.Producto, cantidad, elaborado.Producto.Precio + precioAjuste,
                elaborado.Ubicacion, ObtenerCodigoUnidadMedida(elaborado));
        }

        var insumosOmitidos = opcionesEntity
            .SelectMany(x => x.Ajustes)
            .Where(x => x.TipoAjuste == TiposAjuste.Reemplazo)
            .Select(x => x.Id_Insumo)
            .ToHashSet();

        var costo = 0.00M;

        foreach (var detalleReceta in elaborado.Receta.Detalles)
        {
            if (insumosOmitidos.Contains(detalleReceta.Id_insumo))
                continue;

            var cantidadPorPorcion = detalleReceta.Cantidad / elaborado.Receta.Porciones;
            var cantidadFinal = cantidadPorPorcion * cantidad * (1 + detalleReceta.Merma / 100);

            var totalModificaciones = opcionesEntity
                .SelectMany(x => x.Ajustes)
                .Where(x => x.Id_Insumo == detalleReceta.Id_insumo && x.TipoAjuste == TiposAjuste.Modificacion)
                .Sum(x => x.Cantidad);

            if (totalModificaciones > 0)
                cantidadFinal = (totalModificaciones / elaborado.Receta.Porciones) * cantidad * (1 + detalleReceta.Merma / 100);

            if (detalleReceta.Insumo.Stock_actual < (int)cantidadFinal)
                throw new DetalleRondaException(
                    $"Stock insuficiente para insumo {detalleReceta.Insumo.Nombre}. Disponible: {detalleReceta.Insumo.Stock_actual}, Solicitado: {(int)cantidadFinal}");

            var factor = detalleReceta.Insumo.Factor_conversion > 0 ? detalleReceta.Insumo.Factor_conversion : 1;
            costo += (cantidadFinal * detalleReceta.Insumo.Costo) / factor;

            detalleReceta.Insumo.ComprometerStock((int)cantidadFinal);

            lineas.Add(new CompromisoLineaCalculo
            {
                TipoEntidad = TiposCompromisoInventario.Insumo,
                Id_Insumo = detalleReceta.Id_insumo,
                Cantidad = (int)cantidadFinal,
                Referencia = referencia
            });
        }

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
                throw new DetalleRondaException(
                    $"Stock insuficiente para insumo {insumoNuevo.Nombre}. Disponible: {insumoNuevo.Stock_actual}, Solicitado: {(int)cantidadReemplazo}");

            var factorNuevo = insumoNuevo.Factor_conversion > 0 ? insumoNuevo.Factor_conversion : 1;
            costo += (cantidadReemplazo * insumoNuevo.Costo) / factorNuevo;

            insumoNuevo.ComprometerStock((int)cantidadReemplazo);

            lineas.Add(new CompromisoLineaCalculo
            {
                TipoEntidad = TiposCompromisoInventario.Insumo,
                Id_Insumo = insumoNuevo.Id,
                Cantidad = (int)cantidadReemplazo,
                Referencia = referencia
            });
        }

        var nombre = nombresOpciones.Count > 0
            ? $"{elaborado.Producto.Nombre} ({string.Join(", ", nombresOpciones)})"
            : elaborado.Producto.Nombre;

        lineas.Add(new CompromisoLineaCalculo
        {
            TipoEntidad = TiposCompromisoInventario.Elaborado,
            Id_Producto = idProducto,
            Cantidad = cantidad,
            Costo = costo,
            Referencia = referencia
        });

        var detalle = CrearDetalleRonda(elaborado.Producto, cantidad, elaborado.Producto.Precio + precioAjuste,
            elaborado.Ubicacion, ObtenerCodigoUnidadMedida(elaborado));
        detalle.Nombre_Producto = nombre;
        detalle.Opciones = opcionesEntity.Select(x => new Detalle_Ronda_Opcion
        {
            Id_Opcion = x.Id,
            Opcion = x
        }).ToList();
        return detalle;
    }

    private async Task<Detalle_ronda> ProcesarCombo(
        int idProducto,
        int cantidad,
        string referencia,
        List<CompromisoLineaCalculo> lineas)
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
                {
                    var comprado = detalle.Producto.Comprado;
                    if (comprado is null)
                        throw new DetalleRondaException($"Producto comprado no encontrado en combo: {detalle.Id_Producto}");

                    if (comprado.Stock_actual < cantidadTotal)
                        throw new DetalleRondaException(
                            $"Stock insuficiente para {detalle.Producto.Nombre}. Disponible: {comprado.Stock_actual}, Solicitado: {cantidadTotal}");

                    comprado.ComprometerStock(cantidadTotal);

                    lineas.Add(new CompromisoLineaCalculo
                    {
                        TipoEntidad = TiposCompromisoInventario.Comprado,
                        Id_Producto = detalle.Id_Producto,
                        Cantidad = cantidadTotal,
                        Referencia = referenciaCombo
                    });
                    break;
                }

                case TiposProductos.Elaborado:
                {
                    var elaborado = detalle.Producto.Elaborado;
                    if (elaborado is null)
                        throw new DetalleRondaException($"Elaborado no encontrado en combo: {detalle.Id_Producto}");

                    if (elaborado.Producible)
                    {
                        if (elaborado.Stock_actual < cantidadTotal)
                            throw new DetalleRondaException(
                                $"Stock insuficiente para {detalle.Producto.Nombre}. Disponible: {elaborado.Stock_actual}, Solicitado: {cantidadTotal}");

                        elaborado.ComprometerStock(cantidadTotal);

                        lineas.Add(new CompromisoLineaCalculo
                        {
                            TipoEntidad = TiposCompromisoInventario.Elaborado,
                            Id_Producto = detalle.Id_Producto,
                            Cantidad = cantidadTotal,
                            Referencia = referenciaCombo
                        });
                    }
                    else if (elaborado.Receta is not null)
                    {
                        foreach (var detalleReceta in elaborado.Receta.Detalles)
                        {
                            var cantidadPorPorcion = detalleReceta.Cantidad / elaborado.Receta.Porciones;
                            var cantidadFinal = cantidadPorPorcion * cantidadTotal * (1 + detalleReceta.Merma / 100);

                            if (detalleReceta.Insumo.Stock_actual < (int)cantidadFinal)
                                throw new DetalleRondaException(
                                    $"Stock insuficiente para insumo {detalleReceta.Insumo.Nombre}. Disponible: {detalleReceta.Insumo.Stock_actual}, Solicitado: {(int)cantidadFinal}");

                            detalleReceta.Insumo.ComprometerStock((int)cantidadFinal);

                            lineas.Add(new CompromisoLineaCalculo
                            {
                                TipoEntidad = TiposCompromisoInventario.Insumo,
                                Id_Insumo = detalleReceta.Id_insumo,
                                Cantidad = (int)cantidadFinal,
                                Referencia = referenciaCombo
                            });
                        }
                    }

                    break;
                }
            }
        }

        lineas.Add(new CompromisoLineaCalculo
        {
            TipoEntidad = TiposCompromisoInventario.Promocion,
            Id_Producto = idProducto,
            Cantidad = cantidad,
            Referencia = referencia
        });

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

        var detalleRonda = CrearDetalleRonda(combo.Producto, cantidad, combo.Producto.Precio, string.Empty, UnidadMedidaCombo);
        detalleRonda.ItemsCombo = itemsCombo;
        return detalleRonda;
    }

    private static Detalle_ronda CrearDetalleRonda(
        Producto producto,
        int cantidad,
        decimal precio,
        string ubicacion,
        int codigoUnidadMedida) =>
        new()
        {
            Nombre_Producto = producto.Nombre,
            Cantidad = cantidad,
            Precio = precio,
            Ubicacion = ubicacion,
            Codigo = ObtenerCodigoProducto(producto),
            CodigoSin = ObtenerCodigoSin(producto),
            CodigoUnidadMedida = codigoUnidadMedida
        };

    private static string ObtenerCodigoProducto(Producto producto) =>
        string.IsNullOrWhiteSpace(producto.Codigo)
            ? ProductoCodigoService.Generar(producto.Id)
            : producto.Codigo.Trim();

    private static string ObtenerCodigoSin(Producto producto) =>
        string.IsNullOrWhiteSpace(producto.CodigoSin) ? string.Empty : producto.CodigoSin.Trim();

    private static int ObtenerCodigoUnidadMedida(Comprado? comprado) =>
        comprado?.CodigoUnidadMedida > 0 ? comprado.CodigoUnidadMedida : UnidadMedidaPorDefecto;

    private static int ObtenerCodigoUnidadMedida(Elaborado? elaborado) =>
        elaborado?.CodigoUnidadMedida > 0 ? elaborado.CodigoUnidadMedida : UnidadMedidaPorDefecto;

    private static string UbicacionDeProducto(Producto producto) =>
        producto.Tipo switch
        {
            TiposProductos.Comprado => producto.Comprado?.Ubicacion ?? string.Empty,
            TiposProductos.Elaborado => producto.Elaborado?.Ubicacion ?? string.Empty,
            _ => string.Empty,
        };
}
