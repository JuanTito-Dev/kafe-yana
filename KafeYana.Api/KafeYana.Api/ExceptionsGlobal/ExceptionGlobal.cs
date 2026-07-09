using KafeYana.Application.Exceptions.Usuarios;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;

namespace KafeYana.Application.Exceptions
{
    public class ExceptionGlobal : IExceptionHandler
    {
        private readonly ILogger<ExceptionGlobal> _logger;

        public ExceptionGlobal(ILogger<ExceptionGlobal> _logger)
        {
            this._logger = _logger;
        }

        /// <summary>
        /// Punto de entrada global — intercepta todas las excepciones no manejadas,
        /// convierte errores de Postgres a excepciones del dominio y devuelve la respuesta HTTP
        /// </summary>
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var pgEx = ObtenerPostgresException(exception);

            if (exception is DbUpdateException && pgEx is not null)
            {
                exception = pgEx.SqlState switch
                {
                    "23505" => new UniqueConstraintException(ResolverUnico(pgEx.ConstraintName)),
                    "23503" => new ForeignKeyException(ResolverFK(pgEx.ConstraintName, pgEx.MessageText)),
                    "23001" => new ForeignKeyException(ResolverFK_Eliminacion(pgEx.ConstraintName)),
                    "23502" => new InventarioException($"Campo obligatorio sin valor: {pgEx.ColumnName ?? pgEx.MessageText}"),
                    "22001" => new InventarioException(
                        $"Uno de los valores excede la longitud permitida en base de datos. "
                        + $"Tabla: {pgEx.TableName ?? "?"}. Columna: {pgEx.ColumnName ?? "?"}. "
                        + $"Detalle: {pgEx.MessageText}"),
                    "42703" => new InventarioException("El esquema de base de datos no coincide con la aplicación. Aplique las migraciones pendientes."),

                    _ => exception
                };
            }

            var (statusCode, message) = GetExceptions(exception, pgEx);
            _logger.LogError(exception, exception.Message);
            httpContext.Response.StatusCode = (int)statusCode;
            await httpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken);
            return true;
        }

        private static PostgresException? ObtenerPostgresException(Exception exception)
        {
            for (var actual = exception; actual is not null; actual = actual.InnerException)
            {
                if (actual is PostgresException pgEx)
                    return pgEx;
            }

            return null;
        }

        /// <summary>
        /// Mapea cada tipo de excepción del dominio a su código HTTP correspondiente.
        /// Agregar aquí cada nueva excepción del dominio que se cree en el proyecto
        /// </summary>
        private (HttpStatusCode status, string Message) GetExceptions(Exception exception, PostgresException? pgEx)
        {
            return exception switch
            {
                // ==================== AUTH ====================
                LoginFailException => (HttpStatusCode.Unauthorized, exception.Message),
                RefreshTokenExceptions => (HttpStatusCode.Unauthorized, exception.Message),

                // ==================== USUARIOS ====================
                UsuarioExiste => (HttpStatusCode.Conflict, exception.Message),
                RegiterUsuarioFailException => (HttpStatusCode.BadRequest, exception.Message),

                // ==================== NEGOCIO ====================
                ImagenException => (HttpStatusCode.BadRequest, exception.Message),
                CampoYaExistenteFailException => (HttpStatusCode.Conflict, exception.Message),
                InventarioException => (HttpStatusCode.Conflict, exception.Message),
                DetalleRondaException => (HttpStatusCode.BadRequest, exception.Message),
                OpcionProductoException => (HttpStatusCode.BadRequest, exception.Message),
                CajaException => (HttpStatusCode.Conflict, exception.Message),

                // ==================== BASE DE DATOS ====================
                UniqueConstraintException => (HttpStatusCode.Conflict, exception.Message),
                ForeignKeyException => (HttpStatusCode.BadRequest, exception.Message),
                InvalidOperationException ex when ex.Message.Contains("cannot be tracked")
                            => (HttpStatusCode.Conflict, "El registro ya existe."),
                OrdenCompraException => (HttpStatusCode.BadRequest, exception.Message),
                VentaException => (HttpStatusCode.Conflict, exception.Message),

                DbUpdateException dbEx => (
                    HttpStatusCode.InternalServerError,
                    pgEx?.MessageText ?? dbEx.Message),

                _ => (HttpStatusCode.InternalServerError, $"Ocurrió un error crítico: {exception.Message}")
            };
        }

        /// <summary>
        /// Resuelve errores de violación de índice único (23505).
        /// Agregar aquí cada HasIndex().IsUnique().HasDatabaseName("...") definido en Fluent API
        /// </summary>
        private string ResolverUnico(string? constraintName)
        {
            return constraintName switch
            {
                // ==================== CATEGORIAS ====================
                "ix_categorias_nombre" => "Ya existe una categoría con ese nombre.",
                "fx_nombre_usuario_unico" => "Intente con otro email",

                // ==================== VENTAS ====================
                "Codigo-repetido" => "Ya existe una venta con ese código. Intente de nuevo.",
                "IX_Venta_Cuf" => "Conflicto de CUF: reintenta el cobro en unos segundos.",
                "IX_Venta_NumeroFactura" => "Conflicto de correlativo: reintenta el cobro en unos segundos.",
                "IX_Venta_NumeroFactura_Cafc" => "Conflicto de correlativo CAFC: reintenta el cobro en unos segundos.",
                "PK_Venta" => "Conflicto interno: reintenta el cobro. Si persiste, contacta al administrador.",
                "PK_Detalle_Pago" => "Conflicto interno: reintenta el cobro. Si persiste, contacta al administrador.",

                // ==================== NOTAS DE AJUSTE ====================
                "IX_NotaAjuste_Cuf" => "Conflicto de CUF en nota de ajuste: reintenta la emisión en unos segundos.",
                "IX_NotaAjuste_NumeroNotaCreditoDebito" => "Conflicto de correlativo en nota de ajuste: reintenta la emisión en unos segundos.",

                // ==================== PRODUCTOS ====================
                "id_nombre_producto" => "Ya existe un producto con ese nombre.",
                "codigo_producto_comprado" => "Codigo duplicado envia otro",
                "pk_producto_promocion" => "No puedes agregar un producto duplicado al combo",

                //======================Inusmo=======================
                "nombre_insumo" => "Ya existe un insumo con ese nombre",

                "receta_nombre" => "Ya existe una receta con ese nombre",
                "ix_receta_id_elaborado" => "Ya existe una receta para ese producto elaborado.",

                //variacion
                "fx_varicion_nombre" => "Ya existe una variacion no este nombre para el producto",

                // ==================== PROVEEDORES ====================
                "ix_proveedores_razon_social" => "Ya existe un proveedor con esa razón social.",
                "ix_proveedores_email" => "Ya existe un proveedor con ese email.",
                "ix_proveedores_telefono" => "Ya existe un proveedor con ese teléfono.",
                "ix_proveedores_celular" => "Ya existe un proveedor con ese celular.",

                //======cliente
                "unique_nombre_cliente" => "Ya existe un cliente con este nombre",
                "Unique_celular_cliente" => "Ya existe un cliente con ese número",
                "Unique_correo_cliente" => "Ya existe un cliente con ese correo",
                "Unique_Dni_cliente" => "Ya existe un cliente con este Dni",

                //======puntos
                "ix_aceleradorpuntos_tipo_unique" => "Ya existe un acelerador con ese tipo.",

                //======producto canjeable
                "ix_productocanjeable_producto_unique" => "Este producto ya tiene un registro canjeable.",

                //======promocion permanente
                "ix_promocionpermanente_nombre_unique" => "Ya existe una promoción con ese nombre.",

                //======promocion temporada
                "ix_promociontemporada_nombre_unique" => "Ya existe una promoción por temporada con ese nombre.",
                "pk_promociontemporada_productocanjeable" => "Ese producto canjeable ya está incluido en esta promoción.",
                "ix_historialpromociontemporada_cliente_promocion" => "El cliente ya reclamó esta promoción por temporada.",

                //======hitos por compra
                "ix_hitocompra_numerocompras_unique" => "Ya existe un hito con ese número de compras.",
                "ix_historialhitocompra_cliente_hito" => "El cliente ya reclamó este hito por compras.",

                //Mesa
                "Unique_mesa_nombre" => "Ya existe una mesa con ese nombre",

                _ => "Ya existe un registro con esos datos."
            };
        }

        /// <summary>
        /// Determina si el error FK viene de un INSERT/UPDATE o DELETE
        /// y delega a la función correspondiente según el messageText de Postgres
        /// </summary>
        private string ResolverFK(string? constraintName, string? messageText)
        {
            bool esInsercion = messageText?.Contains("insert or update") ?? false;

            return esInsercion
                ? ResolverFK_Insercion(constraintName)
                : ResolverFK_Eliminacion(constraintName);
        }

        /// <summary>
        /// INSERT/UPDATE: El ID foráneo enviado no existe en la tabla relacionada.
        /// Agregar aquí el nombre de cada .HasConstraintName("...") definido en Fluent API
        /// </summary>
        private string ResolverFK_Insercion(string? constraintName)
        {
            return constraintName switch
            {
                // ==================== PRODUCTOS ====================
                "fk_productos_categoria" => "La categoría seleccionada no existe.",
                "fx_producto_comprado" => "Producto comprado relacion error",
                "fx_producto_elaborado" => "Producto relacion con elaborado",
                "fx_detallecombo_producto" => "Producto no encontrado",
                "fk_detallerondaopcion_detalleronda" => "Detalle ronda no encontrada",
                "fk_detallerondaopcion_opcion" => "Opcion no pertenece al producto",

                //Receta
                "fx_elaborado_receta" => "El producto elaborado seleccionado no existe.",

                //detalle receta 
                "fx_detalle_receta" => "Receta no encontrada",
                "fx_detalle_insumo" => "Insumo no encontrado",

                //variacion
                "fx_variacion_elaborado" => "Error al conectar con el producto",
                "fx_opcion_variacion" => "Error al conectar con variacion",

                //Ajuste
                "fx_ajuste_opcion" => "Erro al encontrar opcion",
                "fx_ajuste_insumobase" => "Insumo base no encontrado",
                "fx_ajuste_insumoNevo" => "Insumo nuevo no encontrado",
                // ==================== PEDIDO ====================
                "id_Pedido_compra" => "El producto seleccionado no existe.",
                "FK_Mesa_Pedido_Id_Pedido" => "El pedido seleccionado no existe.",

                // ==================== DETALLE RONDA ====================
                "FK_Detalle_Ronda_Opcion_Opcion_Id_Opcion" => "La opción seleccionada no existe.",

                //====Ronda
                "fx_pedido_ronda" => "Pedido seleccionado no existe",
                "fx_pedido_mesa" => "Mesa seleccionada no existe",
                "fx_pedido_cliente" => "Cliente seleccionado no existe",
                "fk_detalle_ronda_producto" => "El producto indicado no existe o no es válido para el detalle de la ronda.",
                "FK_Detalle_Ronda_Producto_ProductoId" => "El producto indicado no existe o no es válido para el detalle de la ronda.",
                "fk_detallerondacomboitem_producto" => "El producto del componente de combo no existe.",
                "fk_detallerondacomboitem_detalleronda" => "El detalle de ronda no existe para el ítem de combo.",
                "fx_producto_movimientos" => "Producto no encontrado para registrar el movimiento",
                "fx_insumo_movimiento" => "Insumo no encontrado pára registrar el movimiento",
                "fx_pedido_parallevar" => "Nose pudo conectar con pedido para llevar",
                "fk_cajamovimiento_caja" => "Caja no encontrada para registrar el movimiento",
                "fk_cajahistorialmovimiento_cajahistorial" => "Caja historial no encontrada",
                "fk_ordencompra_proveedor" => "Proveedor no existe para conectar a la orden",
                "fk_ordeniteminsumo_insumo" => "Insumo no encontrado para la orden",
                "fk_ordeniteminsumo_orden" => "Orden de compra no encontrada",
                "fk_ordenitemproducto_producto" => "Producto no encontrado para la orden",
                "fk_productocanjeable_producto" => "El producto seleccionado no existe.",
                "fk_promocionpermanente_productocanjeable" => "El producto canjeable seleccionado no existe.",
                "fk_promtemp_productocanjeable" => "El producto canjeable seleccionado no existe.",
                "fk_historialpromociontemporada_cliente" => "El cliente seleccionado no existe.",
                "fk_historialpromociontemporada_promocion" => "La promoción por temporada seleccionada no existe.",
                "fk_historialhitocompra_cliente" => "El cliente seleccionado no existe.",
                "fk_historialhitocompra_hitocompra" => "El hito por compra seleccionado no existe.",
                "fk_hitocompra_productocanjeable" => "El producto canjeable seleccionado no existe.",
                _ => "El registro relacionado no existe."
            };
        }

        /// <summary>
        /// DELETE: Se intenta eliminar un registro que tiene dependencias (Restrict).
        /// Agregar aquí cada constraint con OnDelete(DeleteBehavior.Restrict) en Fluent API
        /// </summary>
        private string ResolverFK_Eliminacion(string? constraintName)
        {
            return constraintName switch
            {
                // ==================== PRODUCTOS ====================
                "fk_productos_categoria" => "La categoría tiene productos relacionados y no puede eliminarse.",
                "fk_detalle_ronda_producto" => "No se puede eliminar el producto porque está en uno o más pedidos (detalles de ronda).",
                "FK_Detalle_Ronda_Producto_ProductoId" => "No se puede eliminar el producto porque está en uno o más pedidos (detalles de ronda).",
                "fk_detallerondacomboitem_producto" => "No se puede eliminar el producto porque figura en el desglose de un combo en un pedido.",
                "id_Pedido_compra" => "El producto está en un pedido activo y no puede eliminarse.",

                // ==================== CLIENTES ====================
                "fx_pedido_cliente" => "No se puede eliminar el cliente porque tiene pedidos asociados.",

                // ==================== PUNTOS ====================
                "fk_historialpuntos_cliente" => "No se puede eliminar el cliente porque tiene historial de puntos asociados.",

                // ==================== PEDIDO ====================
                "FK_Mesa_Pedido_Id_Pedido" => "El pedido asignado no puede eliminarse.", 
                // ==================== DETALLE RONDA ====================
                "FK_Detalle_Ronda_Opcion_Opcion_Id_Opcion" => "La opción no puede ser eliminada.",
                "fk_detallerondaopcion_opcion" => "No puedes eliminar esta opcion esta en un pedido",
                "fk_pedidoabonodetalle_detalle" => "No se puede eliminar este item porque tiene un cobro parcial vinculado.",

                // ==================== VENTAS ====================
                "Producto asociado a una venta" => "El producto está asociado a una venta y no puede eliminarse.",
                "fk_ordeniteminsumo_insumo"=> "No puede eliminar el insumo esta en una orden de compra",
                "fk_ordenitemproducto_producto" => "No puedes eliminar el producto esta en un orden",
                "fk_promocionpermanente_productocanjeable" => "No puedes eliminar este producto canjeable porque está en una promoción activa.",
                "fk_promtemp_productocanjeable" => "No puedes eliminar este producto canjeable porque está en una promoción por temporada.",
                "fk_historialpromociontemporada_promocion" => "No puedes eliminar esta promoción por temporada porque tiene reclamos registrados.",
                "fk_historialhitocompra_hitocompra" => "No puedes eliminar este hito porque tiene reclamos registrados.",
                "fk_hitocompra_productocanjeable" => "No puedes eliminar este producto canjeable porque está configurado en un hito por compra.",

                _ => "El registro pertenece a otro y no puede eliminarse."
            };
        }
    }
}