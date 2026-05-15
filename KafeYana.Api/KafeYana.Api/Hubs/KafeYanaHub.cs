using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KafeYana.Api.Hubs
{
    [Authorize]
    public class KafeYanaHub : Hub
    {
        private static readonly string[] GruposPermitidos = ["salon", "caja"];

        /// <summary>
        /// El cliente llama este método al conectar para unirse a su área.
        /// Ejemplo: await connection.invoke("UnirseAGrupo", "cocina");
        /// </summary>
        public async Task UnirseAGrupo(string grupo)
        {
            if (!GruposPermitidos.Contains(grupo.ToLower()))
                throw new HubException($"Grupo '{grupo}' no reconocido. Opciones: {string.Join(", ", GruposPermitidos)}");

            await Groups.AddToGroupAsync(Context.ConnectionId, grupo.ToLower());
        }

        public async Task SalirDeGrupo(string grupo)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo.ToLower());
        }
    }
}
