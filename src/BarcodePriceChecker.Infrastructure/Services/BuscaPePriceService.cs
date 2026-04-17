using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Domain.Entities;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace BarcodePriceChecker.Infrastructure.Services;

/// <summary>
/// Busca preços via scraping no Buscapé.
/// AVISO: Web scraping pode violar termos de uso. Use com moderação e respeite robots.txt.
/// Considere substituir por uma API parceira se disponível.
/// </summary>
public class BuscaPePriceService : IPriceSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BuscaPePriceService> _logger;

    public string SourceName => "Buscapé";

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
        try
        {
            var encodedQuery = Uri.EscapeDataString(productName);
            var url = $"https://www.buscape.com.br/search?q={encodedQuery}";

            _logger.LogDebug("Fazendo scraping no Buscapé: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Buscapé retornou status {StatusCode}", response.StatusCode);
                return Enumerable.Empty<PriceOffer>();
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseOffers(html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer scraping no Buscapé para: {ProductName}", productName);
            return Enumerable.Empty<PriceOffer>();
        }
    }

    private IEnumerable<PriceOffer> ParseOffers(string html)
    {
        var offers = new List<PriceOffer>();

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Seletores baseados na estrutura atual do Buscapé
            // IMPORTANTE: Pode precisar de atualização se o site mudar o layout
            var productCards = doc.DocumentNode.SelectNodes("//div[contains(@class,'ProductCard')]");

            if (productCards is null) return offers;

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

                if (!decimal.TryParse(priceText, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var price)) continue;

                offers.Add(new PriceOffer
                {
                    Source = SourceName,
                    ProductName = titleNode?.InnerText.Trim() ?? "Produto",
                    Price = price,
                    Url = linkNode != null ? $"https://www.buscape.com.br{linkNode.GetAttributeValue("href", "")}" : "https://www.buscape.com.br",
                    Seller = "Buscapé",
                    FetchedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception)
        {
            // Parsing failed silently — estrutura HTML pode ter mudado
        }

        return offers;
    }
}
