using System.Net.Http.Headers;
using System.Text.Json;
using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Domain.Entities;
using BarcodePriceChecker.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace BarcodePriceChecker.Infrastructure.Services;

/// <summary>
/// Busca no catálogo de produtos do ML, retornando o preço do buy box.
/// Complementa o MercadoLivrePriceService com resultados do catálogo oficial.
/// </summary>
public class MercadoLivreCatalogService : IPriceSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MercadoLivreCatalogService> _logger;
    private readonly IMercadoLivreTokenService _tokenService;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string SourceName => "Mercado Livre (Catálogo)";

    public MercadoLivreCatalogService(
        HttpClient httpClient,
        ILogger<MercadoLivreCatalogService> logger,
        IMercadoLivreTokenService tokenService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _tokenService = tokenService;
    }

    public async Task<IEnumerable<PriceOffer>> SearchAsync(
        string productName,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _tokenService.GetTokenAsync(cancellationToken);
            if (token is null) return Enumerable.Empty<PriceOffer>();

            var query = !string.IsNullOrWhiteSpace(productName) && productName != barcode
                ? productName
                : barcode;

            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://api.mercadolibre.com/products/search?site_id=MLB&q={encodedQuery}&limit=10";

            _logger.LogDebug("Consultando catálogo ML: {Url}", url);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ML Catálogo retornou {StatusCode}", response.StatusCode);
                return Enumerable.Empty<PriceOffer>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<MercadoLivreProductSearchResponse>(json, JsonOptions);

            if (data?.Results is null || !data.Results.Any())
                return Enumerable.Empty<PriceOffer>();

            return data.Results
                .Where(r => r.BuyBoxWinner?.Price > 0)
                .Select(r => new PriceOffer
                {
                    Source = SourceName,
                    ProductName = r.Name,
                    Price = r.BuyBoxWinner!.Price,
                    Url = $"https://www.mercadolivre.com.br/p/{r.Id}",
                    Seller = "Mercado Livre",
                    FetchedAt = DateTime.UtcNow,
                    IsAvailable = true
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar catálogo ML para: {ProductName}", productName);
            return Enumerable.Empty<PriceOffer>();
        }
    }
}
