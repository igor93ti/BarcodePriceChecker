using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BarcodePriceChecker.Application.Services;

public class PriceComparisonService : IPriceComparisonService
{
    private readonly IBarcodeProductResolver _productResolver;
    private readonly IEnumerable<IPriceSearchService> _priceSearchServices;
    private readonly ILogger<PriceComparisonService> _logger;

    public PriceComparisonService(
        IBarcodeProductResolver productResolver,
        IEnumerable<IPriceSearchService> priceSearchServices,
        ILogger<PriceComparisonService> logger)
    {
        _productResolver = productResolver;
        _priceSearchServices = priceSearchServices;
        _logger = logger;
    }

    public async Task<PriceComparison> CompareAsync(string barcode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando comparação de preços para código de barras: {Barcode}", barcode);

        // 1. Resolve o produto pelo código de barras
        var product = await _productResolver.ResolveAsync(barcode, cancellationToken)
                      ?? new Product { Barcode = barcode, Name = barcode };

        _logger.LogInformation("Produto identificado: {ProductName} ({Brand})", product.Name, product.Brand);

        // 2. Busca preços em paralelo em todas as fontes
        var searchTasks = _priceSearchServices.Select(service =>
            SearchWithFallback(service, product.Name, barcode, cancellationToken));

        var results = await Task.WhenAll(searchTasks);

        var allOffers = results
            .SelectMany(r => r)
            .OrderBy(o => o.Price)
            .ToList();

        _logger.LogInformation("Total de ofertas encontradas: {Count}", allOffers.Count);

        return new PriceComparison
        {
            Product = product,
            Offers = allOffers
        };
    }

    private async Task<IEnumerable<PriceOffer>> SearchWithFallback(
        IPriceSearchService service,
        string productName,
        string barcode,
        CancellationToken cancellationToken)
    {
        try
        {
            return await service.SearchAsync(productName, barcode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao buscar preços em {Source}", service.SourceName);
            return Enumerable.Empty<PriceOffer>();
        }
    }
}
