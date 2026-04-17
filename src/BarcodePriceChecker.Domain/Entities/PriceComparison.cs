namespace BarcodePriceChecker.Domain.Entities;

public class PriceComparison
{
    public Product Product { get; set; } = new();
    public List<PriceOffer> Offers { get; set; } = new();
    public decimal? AveragePrice => Offers.Any() ? Offers.Average(o => o.Price) : null;
    public decimal? LowestPrice => Offers.Any() ? Offers.Min(o => o.Price) : null;
    public decimal? HighestPrice => Offers.Any() ? Offers.Max(o => o.Price) : null;
    public PriceOffer? CheapestOffer => Offers.OrderBy(o => o.Price).FirstOrDefault();

    /// <summary>
    /// Avalia se o preço informado pelo usuário está caro, na média ou barato.
    /// </summary>
    public PriceEvaluation EvaluatePrice(decimal userPrice)
    {
        if (AveragePrice is null) return PriceEvaluation.Unknown;

        var diff = (userPrice - AveragePrice.Value) / AveragePrice.Value * 100;

        return diff switch
        {
            <= -10 => PriceEvaluation.Cheap,
            >= 10 => PriceEvaluation.Expensive,
            _ => PriceEvaluation.Average
        };
    }
}

public enum PriceEvaluation
{
    Unknown,
    Cheap,
    Average,
    Expensive
}
