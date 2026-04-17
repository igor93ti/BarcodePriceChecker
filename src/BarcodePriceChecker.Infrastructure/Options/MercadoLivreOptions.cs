namespace BarcodePriceChecker.Infrastructure.Options;

public class MercadoLivreOptions
{
    public const string SectionName = "MercadoLivre";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
