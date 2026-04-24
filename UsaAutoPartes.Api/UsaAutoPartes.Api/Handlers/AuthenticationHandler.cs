using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using UsaAutoPartes.Application.Exceptions.Autentication;
using UsaAutoPartes.Application.Exceptions.AuthenticationExceptions;
using UsaAutoPartes.Application.Exceptions.GenericExceptions;
using UsaAutoPartes.Domain.Entities.IdentityDb;

namespace UsaAutoPartes.Api.Handlers
{
    public class AuthenticationHandler : IExceptionHandler
    {
        private readonly ILogger<AuthenticationHandler> _logger;
        public AuthenticationHandler(ILogger<AuthenticationHandler> logger)
        {
            _logger = logger;
        }
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statuscode, message) = GetExceptionDetails(exception);

            _logger.LogError(exception, exception.Message);

            httpContext.Response.StatusCode = (int)statuscode;
            await httpContext.Response.WriteAsJsonAsync(new { error = message }, cancellationToken);
    
            return true;
        }

        private (HttpStatusCode statusCode, string message) GetExceptionDetails(Exception exception)
        {
            return exception switch
            {
                LoginFailException => (HttpStatusCode.Unauthorized, exception.Message),
                RefreshTokenFailException => (HttpStatusCode.BadRequest, exception.Message),
                RegistroTransaccionFailException => (HttpStatusCode.BadRequest, exception.Message),
                UsuarioExisteException => (HttpStatusCode.Conflict, exception.Message),
                EntidadNoEncontradaException => (HttpStatusCode.Conflict, exception.Message),
                _ => (HttpStatusCode.InternalServerError, $"Ocurrio un error inesperado. {exception.Message}")
            };
        }
    }
}
