using KafeYana.Core.Entities.Entity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace KafeYana.Api.Middlewares
{
    public class VerificarBloqueoMiddleware
    {
        private readonly RequestDelegate _next;

        public VerificarBloqueoMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<Usuario> userManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var email = context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
                var usuario = await userManager.FindByEmailAsync(email!);

                if (usuario != null && await userManager.IsLockedOutAsync(usuario))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { message = "Usuario bloqueado, contacte al administrador" });
                    return;
                }
            }

            await _next(context);
        }
    }
}
