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
            if (exception is DbUpdateException dbEx &&
                dbEx.InnerException is PostgresException pgEx)
            {
                exception = pgEx.SqlState switch
                {
                    "23505" => new UniqueConstraintException(ResolverUnico(pgEx.ConstraintName)),
                    "23503" => new ForeignKeyException(ResolverFK(pgEx.ConstraintName, pgEx.MessageText)),
                    _ => exception
                };
            }

            var (statusCode, message) = GetExceptions(exception);
            _logger.LogError(exception, exception.Message);
            httpContext.Response.StatusCode = (int)statusCode;
            await httpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken);
            return true;
        }

        /// <summary>
        /// Mapea cada tipo de excepción del dominio a su código HTTP correspondiente.
        /// Agregar aquí cada nueva excepción del dominio que se cree en el proyecto
        /// </summary>
        private (HttpStatusCode status, string Message) GetExceptions(Exception exception)
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
                CampoYaExistenteFailException => (HttpStatusCode.Conflict, exception.Message),
                InventarioException => (HttpStatusCode.Conflict, exception.Message),
                DetalleRondaException => (HttpStatusCode.BadRequest, exception.Message),
                OpcionProductoException => (HttpStatusCode.BadRequest, exception.Message),

                // ==================== BASE DE DATOS ====================
                UniqueConstraintException => (HttpStatusCode.Conflict, exception.Message),
                ForeignKeyException => (HttpStatusCode.BadRequest, exception.Message),
                InvalidOperationException ex when ex.Message.Contains("cannot be tracked")
                            => (HttpStatusCode.Conflict, "El registro ya existe."),

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
                "fx_opcion_nombre" => "Ya existe una opcion con este nombre",

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
                "fx_pedido_ronda" => "Pedido selecionado no existe",
                "fx_pedido_mesa" => "Mesa seleccionada no existe",
                "fx_pedido_cliente" => "Cliente seleccionado no existe",
                "fx_producto_movimientos" => "Producto no encontrado para registrar el Movimineto",
                "fx_insumo_movimiento" => "Insumo no encontrado pára registrar el movimiento",
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
                "id_Pedido_compra" => "El producto está en un pedido activo y no puede eliminarse.",

                // ==================== PEDIDO ====================
                "FK_Mesa_Pedido_Id_Pedido" => "El pedido asignado no puede eliminarse.",

                // ==================== DETALLE RONDA ====================
                "FK_Detalle_Ronda_Opcion_Opcion_Id_Opcion" => "La opción no puede ser eliminada.",

                // ==================== VENTAS ====================
                "Producto asociado a una venta" => "El producto está asociado a una venta y no puede eliminarse.",

                _ => "El registro pertenece a otro y no puede eliminarse."
            };
        }
    }
}