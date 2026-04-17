using System.Text.Json.Serialization;

namespace BarcodePriceChecker.Infrastructure.Http;

public class CosmosProductResponse
{
    [JsonPropertyName("gtin")]
    public long Gtin { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("brand")]
    public CosmosBrand? Brand { get; set; }

    [JsonPropertyName("gpc")]
    public CosmosGpc? Gpc { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("avg_price")]
    public decimal? AvgPrice { get; set; }

    [JsonPropertyName("max_price")]
    public decimal? MaxPrice { get; set; }
}

public class CosmosBrand
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class CosmosGpc
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
