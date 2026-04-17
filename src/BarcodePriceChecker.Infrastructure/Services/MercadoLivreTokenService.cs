using System.Net.Http.Json;
using System.Text.Json;
using BarcodePriceChecker.Infrastructure.Http;
using BarcodePriceChecker.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BarcodePriceChecker.Infrastructure.Services;

public interface IMercadoLivreTokenService
{
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}

public class MercadoLivreTokenService : IMercadoLivreTokenService
{
    private readonly HttpClient _httpClient;
    private readonly MercadoLivreOptions _options;
    private readonly ILogger<MercadoLivreTokenService> _logger;

    private string? _token;
    private DateTime _expiry = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MercadoLivreTokenService(
        HttpClient httpClient,
        IOptions<MercadoLivreOptions> options,
        ILogger<MercadoLivreTokenService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            return null;

        if (_token is not null && DateTime.UtcNow < _expiry)
            return _token;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && DateTime.UtcNow < _expiry)
                return _token;

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret
            });

            var response = await _httpClient.PostAsync(
                "https://api.mercadolibre.com/oauth/token", form, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Falha ao obter token ML: {Status}", response.StatusCode);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<MercadoLivreTokenResponse>(
                JsonOptions, cancellationToken);

            if (tokenResponse?.AccessToken is null) return null;

            _token = tokenResponse.AccessToken;
            _expiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300);

            _logger.LogInformation("Token Mercado Livre obtido. Expira em {Expiry:HH:mm:ss}", _expiry);
            return _token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter token do Mercado Livre");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }
}
