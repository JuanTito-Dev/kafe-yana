using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

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

        public async Task<JsonElement> DispararCumpleanosAsync()
        {
            _logger.LogInformation("Disparando webhook de cumpleaños...");
            var response = await _httpClient.PostAsync("/webhook/cumpleanos", null);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Python devolvió {StatusCode}: {Content}", response.StatusCode, content);

            return JsonSerializer.Deserialize<JsonElement>(content);
        }

        public async Task<JsonElement> DispararTemporadaAsync()
        {
            _logger.LogInformation("Disparando webhook de temporada...");
            var response = await _httpClient.PostAsync("/webhook/temporada", null);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Python devolvió {StatusCode}: {Content}", response.StatusCode, content);

            return JsonSerializer.Deserialize<JsonElement>(content);
        }

        public async Task<JsonElement> DispararPuntosAsync()
        {
            _logger.LogInformation("Disparando webhook de puntos...");
            var response = await _httpClient.PostAsync("/webhook/venta", null);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Python devolvió {StatusCode}: {Content}", response.StatusCode, content);

            return JsonSerializer.Deserialize<JsonElement>(content);
        }

        public async Task<JsonElement> DispararPermanentesAsync()
        {
            _logger.LogInformation("Disparando webhook de permanentes...");
            var response = await _httpClient.PostAsync("/webhook/productospermanentes", null);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Python devolvió {StatusCode}: {Content}", response.StatusCode, content);

            return JsonSerializer.Deserialize<JsonElement>(content);
        }
    }
}