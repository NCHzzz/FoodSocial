using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;

namespace FoodSocial.Bff;

public interface IInternalServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

internal sealed class KeycloakClientCredentialsTokenProvider(
    HttpClient httpClient,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<KeycloakClientCredentialsTokenProvider> logger)
    : IInternalServiceTokenProvider
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly ILogger<KeycloakClientCredentialsTokenProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAtUtc;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var staticToken = _configuration["Keycloak:ServiceClientAccessToken"];
        if (!string.IsNullOrWhiteSpace(staticToken))
        {
            if (!_environment.IsDevelopment())
            {
                _logger.LogError(
                    "Keycloak:ServiceClientAccessToken is configured in environment '{Environment}' which is not Development. Ignoring the static token and falling through to the dynamic client-credentials flow. Remove the static token from production configuration.",
                    _environment.EnvironmentName);
            }
            else
            {
                _logger.LogWarning(
                    "Keycloak:ServiceClientAccessToken is configured in Development. Using the static token and skipping the dynamic client-credentials flow. Remove the static token before deploying to non-development environments.");
                return staticToken;
            }
        }

        if (!string.IsNullOrWhiteSpace(_accessToken) && _expiresAtUtc > DateTimeOffset.UtcNow.AddSeconds(30))
        {
            return _accessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && _expiresAtUtc > DateTimeOffset.UtcNow.AddSeconds(30))
            {
                return _accessToken;
            }

            var tokenEndpoint = ResolveTokenEndpoint();
            var clientId = _configuration["Keycloak:ServiceClientId"]
                ?? throw new InvalidOperationException("Keycloak service client id is not configured.");
            var clientSecret = _configuration["Keycloak:ServiceClientSecret"]
                ?? throw new InvalidOperationException("Keycloak service client secret is not configured.");

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret
                })
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Keycloak client-credentials token request failed with {StatusCode}. Response body: {ResponseBody}",
                    (int)response.StatusCode,
                    body);
                throw new InvalidOperationException("Keycloak service client token request failed.");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<ClientCredentialsTokenResponse>(cancellationToken);
            if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Keycloak service client token response was empty.");
            }

            _accessToken = tokenResponse.AccessToken;
            _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(tokenResponse.ExpiresIn, 60));
            return _accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private string ResolveTokenEndpoint()
    {
        var configuredEndpoint = _configuration["Keycloak:TokenEndpoint"];
        if (!string.IsNullOrWhiteSpace(configuredEndpoint))
        {
            return configuredEndpoint;
        }

        var authority = _configuration["Keycloak:Authority"];
        if (!string.IsNullOrWhiteSpace(authority))
        {
            return $"{authority.TrimEnd('/')}/protocol/openid-connect/token";
        }

        var realm = _configuration["Keycloak:Realm"]
            ?? throw new InvalidOperationException("Keycloak realm is not configured.");
        var discoveredAuthority = _configuration["Services:keycloak:http:0"]
            ?? _configuration["Services:keycloak:https:0"]
            ?? throw new InvalidOperationException("Keycloak authority is not configured. Set Keycloak:Authority or provide an Aspire service reference for 'keycloak'.");

        return $"{discoveredAuthority.TrimEnd('/')}/realms/{realm}/protocol/openid-connect/token";
    }

    private sealed record ClientCredentialsTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

internal static class InternalServiceTokenProviderRegistration
{
    public static IServiceCollection AddInternalServiceTokenProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpClient<IInternalServiceTokenProvider, KeycloakClientCredentialsTokenProvider>();
        return services;
    }
}
