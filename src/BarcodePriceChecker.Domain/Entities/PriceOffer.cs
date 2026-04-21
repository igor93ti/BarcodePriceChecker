namespace BarcodePriceChecker.Domain.Entities;

public class PriceOffer
{
    public string Source { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Seller { get; set; } = string.Empty;
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    public bool IsAvailable { get; set; } = true;
}
