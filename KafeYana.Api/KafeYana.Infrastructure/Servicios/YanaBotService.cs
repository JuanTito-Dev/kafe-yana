using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace KafeYana.Infrastructure.Servicios
{
    public class YanaBotService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YanaBotService> _logger;

        public YanaBotService(HttpClient httpClient, ILogger<YanaBotService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> DispararCumpleanosAsync()
        {
            _logger.LogInformation("Disparando webhook de cumpleaños...");
            var response = await _httpClient.PostAsync("/webhook/cumpleanos", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> DispararTemporadaAsync()
        {
            _logger.LogInformation("Disparando webhook de temporada...");
            var response = await _httpClient.PostAsync("/webhook/temporada", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}