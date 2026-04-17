using System.Text.Json;
using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Domain.Entities;
using BarcodePriceChecker.Infrastructure.Http;
using BarcodePriceChecker.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BarcodePriceChecker.Infrastructure.Services;

/// <summary>
/// Resolve produtos usando a API Bluesoft Cosmos (base brasileira de GTIN/EAN).
/// https://cosmos.bluesoft.com.br/api
/// </summary>
public class CosmosProductResolver : IBarcodeProductResolver
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CosmosProductResolver> _logger;
    private readonly CosmosOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CosmosProductResolver(
        HttpClient httpClient,
        ILogger<CosmosProductResolver> logger,
        IOptions<CosmosOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<Product?> ResolveAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogDebug("Cosmos token não configurado — pulando.");
            return null;
        }

        try
        {
            var url = $"https://api.cosmos.bluesoft.com.br/gtins/{barcode}.json";
            _logger.LogDebug("Consultando Cosmos: {Url}", url);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Cosmos-Token", _options.Token);
            request.Headers.Add("User-Agent", "Cosmos-API-Request");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Produto não encontrado no Cosmos para barcode: {Barcode}", barcode);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Cosmos retornou {Status} para {Barcode}", response.StatusCode, barcode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<CosmosProductResponse>(json, JsonOptions);

            if (data is null || string.IsNullOrWhiteSpace(data.Description))
                return null;

            return new Product
            {
                Barcode = barcode,
                Name = data.Description.Trim(),
                Brand = data.Brand?.Name?.Trim() ?? string.Empty,
                Category = data.Gpc?.Description?.Trim() ?? string.Empty,
                ImageUrl = data.Thumbnail ?? string.Empty,
                Description = data.Description.Trim()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar Cosmos para barcode: {Barcode}", barcode);
            return null;
        }
    }
}
