using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BarcodePriceChecker.Infrastructure.Services;

/// <summary>
/// Tenta resolver o produto em múltiplas fontes, na ordem configurada.
/// Retorna o primeiro resultado não-nulo.
/// </summary>
public class ChainedProductResolver : IBarcodeProductResolver
{
    private readonly IReadOnlyList<IBarcodeProductResolver> _resolvers;
    private readonly ILogger<ChainedProductResolver> _logger;

    public ChainedProductResolver(
        IEnumerable<IBarcodeProductResolver> resolvers,
        ILogger<ChainedProductResolver> logger)
    {
        _resolvers = resolvers.ToList();
        _logger = logger;
    }

    public async Task<Product?> ResolveAsync(string barcode, CancellationToken cancellationToken = default)
    {
        foreach (var resolver in _resolvers)
        {
            var result = await resolver.ResolveAsync(barcode, cancellationToken);
            if (result is not null)
            {
                _logger.LogInformation("Produto resolvido por {Resolver}: {Name}",
                    resolver.GetType().Name, result.Name);
                return result;
            }
        }

        _logger.LogWarning("Nenhum resolver encontrou produto para barcode: {Barcode}", barcode);
        return null;
    }
}
