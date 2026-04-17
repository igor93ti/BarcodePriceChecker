using BarcodePriceChecker.Domain.Entities;

namespace BarcodePriceChecker.Application.Interfaces;

/// <summary>
/// Contrato para qualquer provedor de busca de preços.
/// Implementações: MercadoLivre, BuscaPe, etc.
/// </summary>
public interface IPriceSearchService
{
    string SourceName { get; }
    Task<IEnumerable<PriceOffer>> SearchAsync(string productName, string barcode, CancellationToken cancellationToken = default);
}
