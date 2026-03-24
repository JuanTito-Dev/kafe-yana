using KafeYana.Application.Exceptions.Usuarios;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
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
            var (statuscode, message) = GetExceptions(exception);

            _logger.LogError(exception, exception.Message);

            httpContext.Response.StatusCode = (int)statuscode;

            await httpContext.Response.WriteAsJsonAsync(message, cancellationToken);

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
                _ => (HttpStatusCode.InternalServerError, $"Ocurrido un error critico {exception.Message}")
            };
        }
    }
}
