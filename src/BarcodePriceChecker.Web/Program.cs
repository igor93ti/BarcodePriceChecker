using BarcodePriceChecker.Infrastructure;
using BarcodePriceChecker.Web.Components;
using Prometheus;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Blazor Server
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Infraestrutura (scrapers, APIs externas)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Cache em memória para evitar buscas repetidas
    builder.Services.AddMemoryCache();

    // Health checks
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAntiforgery();

    // Prometheus metrics endpoint: /metrics
    app.UseHttpMetrics();
    app.MapMetrics();

    // Health check endpoint: /health
    app.MapHealthChecks("/health");

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
