using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Application.Services;
using BarcodePriceChecker.Infrastructure.Options;
using BarcodePriceChecker.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BarcodePriceChecker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MercadoLivreOptions>(
            configuration.GetSection(MercadoLivreOptions.SectionName));

        services.AddHttpClient<OpenFoodFactsProductResolver>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "BarcodePriceChecker/1.0 (contact: dev@example.com)");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient("MercadoLivreToken", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<MercadoLivrePriceService>(client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "BarcodePriceChecker/1.0 (contact: dev@example.com)");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<MercadoLivreCatalogService>(client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<BuscaPePriceService>(client =>
        {
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddSingleton<IMercadoLivreTokenService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("MercadoLivreToken");
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MercadoLivreOptions>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MercadoLivreTokenService>>();
            return new MercadoLivreTokenService(httpClient, options, logger);
        });
        services.AddScoped<IBarcodeProductResolver, OpenFoodFactsProductResolver>();
        services.AddScoped<IPriceSearchService, MercadoLivrePriceService>();
        services.AddScoped<IPriceSearchService, MercadoLivreCatalogService>();
        services.AddScoped<IPriceSearchService, BuscaPePriceService>();
        services.AddScoped<IPriceComparisonService, PriceComparisonService>();

        return services;
    }
}
