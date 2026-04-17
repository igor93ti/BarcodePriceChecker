using BarcodePriceChecker.Application.Interfaces;
using BarcodePriceChecker.Application.Services;
using BarcodePriceChecker.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BarcodePriceChecker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // HttpClients com headers padrão
        services.AddHttpClient<OpenFoodFactsProductResolver>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "BarcodePriceChecker/1.0 (contact: dev@example.com)");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<MercadoLivrePriceService>(client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<BuscaPePriceService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Registro das interfaces
        services.AddScoped<IBarcodeProductResolver, OpenFoodFactsProductResolver>();
        services.AddScoped<IPriceSearchService, MercadoLivrePriceService>();
        services.AddScoped<IPriceSearchService, BuscaPePriceService>();
        services.AddScoped<IPriceComparisonService, PriceComparisonService>();

        return services;
    }
}
