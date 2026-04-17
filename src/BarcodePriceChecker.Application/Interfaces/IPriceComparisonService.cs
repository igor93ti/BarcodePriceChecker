using BarcodePriceChecker.Domain.Entities;

namespace BarcodePriceChecker.Application.Interfaces;

public interface IPriceComparisonService
{
    Task<PriceComparison> CompareAsync(string barcode, CancellationToken cancellationToken = default);
}
