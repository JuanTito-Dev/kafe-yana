using KafeYana.Application.Exceptions.Usuarios;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is DbUpdateException dbEx &&
                dbEx.InnerException is PostgresException pgEx)
            {
                exception = pgEx.SqlState switch
                {
                    "23505" => new UniqueConstraintException(ResolverUnico(pgEx.ConstraintName)),
                    "23503" => new ForeignKeyException(ResolverFK(pgEx.ConstraintName)),
                    _ => exception
                };
            }

            var (statuscode, message) = GetExceptions(exception);
            _logger.LogError(exception, exception.Message);
            httpContext.Response.StatusCode = (int)statuscode;
            await httpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken);
            return true;
        }

        private (HttpStatusCode status, string Message) GetExceptions(Exception exception)
        {
            return exception switch
            {
                LoginFailException => (HttpStatusCode.Unauthorized, exception.Message),
                UsuarioExiste => (HttpStatusCode.Conflict, exception.Message),
                RegiterUsuarioFailException => (HttpStatusCode.BadRequest, exception.Message),
                RefreshTokenExceptions => (HttpStatusCode.Unauthorized, exception.Message),
                InventarioException => (HttpStatusCode.Conflict, exception.Message),
                DetalleRondaException => (HttpStatusCode.BadRequest, exception.Message),
                OpcionProductoException => (HttpStatusCode.BadRequest, exception.Message),
                CampoYaExistenteFailException => (HttpStatusCode.Conflict, exception.Message),
                UniqueConstraintException => (HttpStatusCode.Conflict, exception.Message),
                ForeignKeyException => (HttpStatusCode.BadRequest, exception.Message),
                _ => (HttpStatusCode.InternalServerError, $"Ocurrió un error crítico: {exception.Message}")
            };
        }

        private string ResolverUnico(string? constraintName)
        {
            return constraintName switch
            {
                "Codigo-repetido" => "El código ya existe.",
                "ix_categorias_nombre" => "Ya existe una categoría con ese nombre.",
                // Proveedores
                "ix_proveedores_razon_social" => "Ya existe un proveedor con esa razón social.",
                "ix_proveedores_email" => "Ya existe un proveedor con ese email.",
                "ix_proveedores_telefono" => "Ya existe un proveedor con ese teléfono.",
                "ix_proveedores_celular" => "Ya existe un proveedor con ese celular.",
                _ => "Ya existe un registro con esos datos."
            };
        }

        private string ResolverFK(string? constraintName)
        {
            return constraintName switch
            {
                "FK_Mesa_Pedido_Id_Pedido" => "El pedido asignado no existe.",
                "Producto asociado a una venta" => "El producto está asociado a una venta y no puede ser eliminado.",
                _ => "La referencia indicada no existe."
            };
        }
    }
}
