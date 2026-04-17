using BarcodePriceChecker.Domain.Entities;

namespace BarcodePriceChecker.Application.Interfaces;

/// <summary>
/// Resolve informações de um produto a partir do código de barras.
/// Usa a API Open Food Facts (gratuita, sem autenticação).
/// </summary>
public interface IBarcodeProductResolver
{
    Task<Product?> ResolveAsync(string barcode, CancellationToken cancellationToken = default);
}
