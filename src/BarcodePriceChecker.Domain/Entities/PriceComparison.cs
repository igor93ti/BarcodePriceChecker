namespace BarcodePriceChecker.Domain.Entities;

public class PriceComparison
{
    public Product Product { get; set; } = new();
    public List<PriceOffer> Offers { get; set; } = new();
    public decimal? AveragePrice => Offers.Any() ? Offers.Average(o => o.Price) : null;
    public decimal? LowestPrice => Offers.Any() ? Offers.Min(o => o.Price) : null;
    public decimal? HighestPrice => Offers.Any() ? Offers.Max(o => o.Price) : null;
    public decimal? MedianPrice => CalculateMedian(ComparablePrices);
    public decimal? TypicalLowPrice => CalculatePercentile(ComparablePrices, 0.25m);
    public decimal? TypicalHighPrice => CalculatePercentile(ComparablePrices, 0.75m);
    public decimal? ReferencePrice => MedianPrice ?? AveragePrice;
    public int SourceCount => Offers.Select(o => o.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    public PriceOffer? CheapestOffer => Offers.OrderBy(o => o.Price).FirstOrDefault();

    public PriceConfidence Confidence => Offers.Count switch
    {
        >= 10 when SourceCount >= 2 => PriceConfidence.High,
        >= 5 => PriceConfidence.Medium,
        > 0 => PriceConfidence.Low,
        _ => PriceConfidence.None
    };

    public PriceEvaluation EvaluatePrice(decimal userPrice)
    {
        if (ReferencePrice is null) return PriceEvaluation.Unknown;

        var diff = CalculateDifferenceFromReference(userPrice);

        return diff switch
        {
            <= -10 => PriceEvaluation.Cheap,
            >= 10 => PriceEvaluation.Expensive,
            _ => PriceEvaluation.Average
        };
    }

    public decimal? CalculateDifferenceFromReference(decimal userPrice)
    {
        if (ReferencePrice is null || ReferencePrice.Value == 0) return null;

        return (userPrice - ReferencePrice.Value) / ReferencePrice.Value * 100;
    }

    private List<decimal> ComparablePrices
    {
        get
        {
            var prices = Offers
                .Where(o => o.IsAvailable && o.Price > 0)
                .Select(o => o.Price)
                .Order()
                .ToList();

            if (prices.Count < 4) return prices;

            var median = CalculateMedian(prices);
            if (median is null || median.Value == 0) return prices;

            return prices
                .Where(price => price >= median.Value * 0.5m && price <= median.Value * 2m)
                .ToList();
        }
    }

    private static decimal? CalculateMedian(IReadOnlyList<decimal> prices)
    {
        if (prices.Count == 0) return null;

        var middle = prices.Count / 2;
        return prices.Count % 2 == 0
            ? (prices[middle - 1] + prices[middle]) / 2
            : prices[middle];
    }

    private static decimal? CalculatePercentile(IReadOnlyList<decimal> prices, decimal percentile)
    {
        if (prices.Count == 0) return null;
        if (prices.Count == 1) return prices[0];

        var position = (prices.Count - 1) * percentile;
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);

        if (lowerIndex == upperIndex) return prices[lowerIndex];

        var weight = position - lowerIndex;
        return prices[lowerIndex] + (prices[upperIndex] - prices[lowerIndex]) * weight;
    }
}

public enum PriceEvaluation
{
    Unknown,
    Cheap,
    Average,
    Expensive
}

public enum PriceConfidence
{
    None,
    Low,
    Medium,
    High
}
