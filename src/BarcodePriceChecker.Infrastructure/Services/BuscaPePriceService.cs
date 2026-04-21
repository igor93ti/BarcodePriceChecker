using System.Globalization;
using System.Text.Json;
using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Domain.Entities;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace BarcodePriceChecker.Infrastructure.Services;

public class BuscaPePriceService : IPriceSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BuscaPePriceService> _logger;

    public string SourceName => "Buscape";

    public BuscaPePriceService(HttpClient httpClient, ILogger<BuscaPePriceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<PriceOffer>> SearchAsync(
        string productName,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productName) || productName == barcode)
            return Enumerable.Empty<PriceOffer>();

        try
        {
            var encodedQuery = Uri.EscapeDataString(productName);
            var url = $"https://www.buscape.com.br/search?q={encodedQuery}";

            _logger.LogDebug("Consultando Buscape: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Buscape retornou {StatusCode} para: {ProductName}", response.StatusCode, productName);
                return Enumerable.Empty<PriceOffer>();
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseOffers(html, barcode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar Buscape para: {ProductName}", productName);
            return Enumerable.Empty<PriceOffer>();
        }
    }

    private IEnumerable<PriceOffer> ParseOffers(string html, string barcode)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var fromNextData = ParseNextDataOffers(doc, barcode).ToList();
        if (fromNextData.Any()) return fromNextData;

        return ParseHtmlCards(doc, barcode).ToList();
    }

    private IEnumerable<PriceOffer> ParseNextDataOffers(HtmlDocument doc, string barcode)
    {
        var scriptNode = doc.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']");
        if (scriptNode is null) return Enumerable.Empty<PriceOffer>();

        try
        {
            using var json = JsonDocument.Parse(scriptNode.InnerText);

            if (!TryGetProperty(json.RootElement, "props", out var props) ||
                !TryGetProperty(props, "pageProps", out var pageProps) ||
                !TryGetProperty(pageProps, "initialReduxState", out var state) ||
                !TryGetProperty(state, "hits", out var hitsState) ||
                !TryGetProperty(hitsState, "hits", out var hits) ||
                hits.ValueKind != JsonValueKind.Array)
            {
                return Enumerable.Empty<PriceOffer>();
            }

            return hits.EnumerateArray()
                .Select(hit => ParseNextDataOffer(hit, barcode))
                .Where(offer => offer is not null)
                .Cast<PriceOffer>()
                .Take(5)
                .ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Falha ao ler JSON do Buscape.");
            return Enumerable.Empty<PriceOffer>();
        }
    }

    private PriceOffer? ParseNextDataOffer(JsonElement hit, string barcode)
    {
        if (!TryGetDecimal(hit, "price", out var price) || price <= 0)
            return null;

        var title = TryGetString(hit, "name", out var name) ? name : "Produto";
        var seller = TryGetString(hit, "merchantName", out var merchantName) ? merchantName : SourceName;
        var url = TryGetString(hit, "url", out var offerUrl) ? offerUrl : string.Empty;

        return new PriceOffer
        {
            Source = SourceName,
            ProductName = title,
            Price = price,
            Barcode = barcode,
            Url = BuildBuscaPeUrl(url),
            Seller = seller,
            FetchedAt = DateTime.UtcNow,
            IsAvailable = true
        };
    }

    private IEnumerable<PriceOffer> ParseHtmlCards(HtmlDocument doc, string barcode)
    {
        var productCards = doc.DocumentNode.SelectNodes("//div[contains(@class,'ProductCard')]");
        if (productCards is null) return Enumerable.Empty<PriceOffer>();

        var offers = new List<PriceOffer>();
        foreach (var card in productCards.Take(5))
        {
            var titleNode = card.SelectSingleNode(".//h2 | .//h3 | .//*[contains(@class,'title')]");
            var priceNode = card.SelectSingleNode(".//*[contains(@class,'price')] | .//*[contains(@class,'Price')]");
            var linkNode = card.SelectSingleNode(".//a[@href]");

            if (priceNode is null) continue;

            var priceText = priceNode.InnerText.Trim()
                .Replace("R$", "")
                .Replace(".", "")
                .Replace(",", ".")
                .Trim();

            if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                continue;

            offers.Add(new PriceOffer
            {
                Source = SourceName,
                ProductName = titleNode?.InnerText.Trim() ?? "Produto",
                Price = price,
                Barcode = barcode,
                Url = BuildBuscaPeUrl(linkNode?.GetAttributeValue("href", "") ?? string.Empty),
                Seller = SourceName,
                FetchedAt = DateTime.UtcNow,
                IsAvailable = true
            });
        }

        return offers;
    }

    private static string BuildBuscaPeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "https://www.buscape.com.br";
        if (Uri.TryCreate(url, UriKind.Absolute, out _)) return url;

        return $"https://www.buscape.com.br{url}";
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value);
    }

    private static bool TryGetString(JsonElement element, string name, out string value)
    {
        value = string.Empty;

        if (!TryGetProperty(element, name, out var property) || property.ValueKind != JsonValueKind.String)
            return false;

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetDecimal(JsonElement element, string name, out decimal value)
    {
        value = 0;
        return TryGetProperty(element, name, out var property) && property.TryGetDecimal(out value);
    }
}
