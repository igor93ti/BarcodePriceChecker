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

public class MercadoLivreTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public class MercadoLivreProductSearchResponse
{
    [JsonPropertyName("results")]
    public List<MercadoLivreProduct> Results { get; set; } = new();
}

public class MercadoLivreProduct
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("buy_box_winner")]
    public MercadoLivreBuyBoxWinner? BuyBoxWinner { get; set; }
}

public class MercadoLivreBuyBoxWinner
{
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = string.Empty;
}
