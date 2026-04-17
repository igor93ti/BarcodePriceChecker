using System.Text.Json;
using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Domain.Entities;
using BarcodePriceChecker.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace BarcodePriceChecker.Infrastructure.Services;

/// <summary>
/// Resolve informações do produto usando a API gratuita Open Food Facts.
/// Documentação: https://world.openfoodfacts.org/data
/// Endpoint: GET https://world.openfoodfacts.org/api/v0/product/{barcode}.json
/// </summary>
public class OpenFoodFactsProductResolver : IBarcodeProductResolver
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsProductResolver> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OpenFoodFactsProductResolver(HttpClient httpClient, ILogger<OpenFoodFactsProductResolver> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Product?> ResolveAsync(string barcode, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://world.openfoodfacts.org/api/v0/product/{barcode}.json";
            _logger.LogDebug("Consultando Open Food Facts: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<OpenFoodFactsResponse>(json, JsonOptions);

            if (data?.Status != 1 || data.Product is null)
            {
                _logger.LogWarning("Produto não encontrado no Open Food Facts para barcode: {Barcode}", barcode);
                return null;
            }

            var p = data.Product;
            return new Product
            {
                Barcode = barcode,
                Name = p.ProductNamePt ?? p.ProductName ?? p.GenericName ?? barcode,
                Brand = p.Brands?.Split(',').FirstOrDefault()?.Trim() ?? string.Empty,
                Category = p.Categories?.Split(',').FirstOrDefault()?.Trim() ?? string.Empty,
                ImageUrl = p.ImageUrl ?? string.Empty,
                Description = p.GenericName ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar Open Food Facts para barcode: {Barcode}", barcode);
            return null;
        }
    }
}
