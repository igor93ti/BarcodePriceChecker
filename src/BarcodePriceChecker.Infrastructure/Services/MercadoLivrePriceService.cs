using System.Text.Json;
using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Domain.Entities;
using BarcodePriceChecker.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace BarcodePriceChecker.Infrastructure.Services;

/// <summary>
/// Busca preços na API pública do Mercado Livre (sem autenticação necessária).
/// Documentação: https://developers.mercadolivre.com.br/
/// Endpoint: GET https://api.mercadolibre.com/sites/MLB/search?q={query}&limit=10
/// </summary>
public class MercadoLivrePriceService : IPriceSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MercadoLivrePriceService> _logger;

    public string SourceName => "Mercado Livre";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MercadoLivrePriceService(HttpClient httpClient, ILogger<MercadoLivrePriceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<PriceOffer>> SearchAsync(
        string productName,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Tenta primeiro pelo código de barras, depois pelo nome
            var results = await SearchByQuery(barcode, cancellationToken);

            if (!results.Any() && !string.IsNullOrWhiteSpace(productName) && productName != barcode)
            {
                results = await SearchByQuery(productName, cancellationToken);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar preços no Mercado Livre para: {ProductName}", productName);
            return Enumerable.Empty<PriceOffer>();
        }
    }

    private async Task<List<PriceOffer>> SearchByQuery(string query, CancellationToken cancellationToken)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://api.mercadolibre.com/sites/MLB/search?q={encodedQuery}&limit=10&condition=new";

        _logger.LogDebug("Consultando Mercado Livre: {Url}", url);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<MercadoLivreSearchResponse>(json, JsonOptions);

        if (data?.Results is null || !data.Results.Any())
            return new List<PriceOffer>();

        return data.Results
            .Where(r => r.Price > 0 && r.AvailableQuantity > 0)
            .Select(r => new PriceOffer
            {
                Source = SourceName,
                ProductName = r.Title,
                Price = r.Price,
                Url = r.Permalink,
                Seller = r.Seller?.Nickname ?? "Vendedor ML",
                FetchedAt = DateTime.UtcNow,
                IsAvailable = true
            })
            .ToList();
    }
}
