using System.Text.Json.Serialization;

namespace BarcodePriceChecker.Infrastructure.Http;

public class MercadoLivreSearchResponse
{
    [JsonPropertyName("results")]
    public List<MercadoLivreItem> Results { get; set; } = new();

    [JsonPropertyName("paging")]
    public MercadoLivrePaging? Paging { get; set; }
}

public class MercadoLivreItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("permalink")]
    public string Permalink { get; set; } = string.Empty;

    [JsonPropertyName("seller")]
    public MercadoLivreSeller? Seller { get; set; }

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("available_quantity")]
    public int AvailableQuantity { get; set; }
}

public class MercadoLivreSeller
{
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
}

public class MercadoLivrePaging
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
}
