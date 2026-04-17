using System.Net.Http.Headers;
using System.Text.Json;
using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Domain.Entities;
using BarcodePriceChecker.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace BarcodePriceChecker.Infrastructure.Services;

public class MercadoLivrePriceService : IPriceSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MercadoLivrePriceService> _logger;
    private readonly IMercadoLivreTokenService _tokenService;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string SourceName => "Mercado Livre";

    public MercadoLivrePriceService(
        HttpClient httpClient,
        ILogger<MercadoLivrePriceService> logger,
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
            if (token is null)
            {
                _logger.LogWarning("Credenciais ML não configuradas. Defina MercadoLivre:ClientId e MercadoLivre:ClientSecret.");
                return Enumerable.Empty<PriceOffer>();
            }

            // Prioriza busca por nome do produto (muito mais resultados)
            // Cai para busca por código de barras apenas se o nome não estiver disponível
            var hasName = !string.IsNullOrWhiteSpace(productName) && productName != barcode;
            var results = hasName
                ? await SearchByQuery(productName, token, cancellationToken)
                : new List<PriceOffer>();

            if (!results.Any())
                results = await SearchByQuery(barcode, token, cancellationToken);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar preços no Mercado Livre para: {ProductName}", productName);
            return Enumerable.Empty<PriceOffer>();
        }
    }

    private async Task<List<PriceOffer>> SearchByQuery(
        string query, string token, CancellationToken cancellationToken)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://api.mercadolibre.com/sites/MLB/search?q={encodedQuery}&limit=10&condition=new";

        _logger.LogDebug("Consultando Mercado Livre: {Url}", url);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mercado Livre retornou {StatusCode} para: {Query}", response.StatusCode, query);
            return new List<PriceOffer>();
        }

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
