using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;



namespace FoodSocial.Bff;

/// <summary>
/// Port at which the BFF obtains a Bearer access token for calling
/// downstream service APIs (Post, UserProfile, Dish, Place, Identity).
/// </summary>
public interface IInternalServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Production adapter: runs the OAuth2 client_credentials grant against the
/// Keycloak token endpoint, caches the access token, and refreshes on
/// expiration. No static-token escape hatch lives in this module — the
/// launch posture is "no static token in production."
/// </summary>
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
        if (!_environment.IsDevelopment())
        {
            var staticToken = _configuration["Keycloak:ServiceClientAccessToken"];
            if (!string.IsNullOrWhiteSpace(staticToken))
            {
                _logger.LogError(
                    "Keycloak:ServiceClientAccessToken is configured in environment '{Environment}' which is not Development. Refusing to use the static token. Remove the static token from production configuration.",
                    _environment.EnvironmentName);
                throw new InvalidOperationException(
                    $"Keycloak:ServiceClientAccessToken must not be set in environment '{_environment.EnvironmentName}'.");
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

/// <summary>
/// Development-only adapter: returns the static
/// <c>Keycloak:ServiceClientAccessToken</c> from configuration without ever
/// calling the Keycloak token endpoint. The launch posture keeps this
/// adapter out of production wiring; the registration in
/// <see cref="InternalServiceTokenProviderRegistration"/> rejects it when the
/// environment is not Development.
/// </summary>
internal sealed class StaticInternalServiceTokenProvider(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<StaticInternalServiceTokenProvider> logger)
    : IInternalServiceTokenProvider
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly ILogger<StaticInternalServiceTokenProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _staticToken = configuration["Keycloak:ServiceClientAccessToken"]
        ?? throw new InvalidOperationException("Keycloak:ServiceClientAccessToken must be set when using StaticInternalServiceTokenProvider.");

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"StaticInternalServiceTokenProvider must not be used in environment '{_environment.EnvironmentName}'.");
        }

        _logger.LogDebug("StaticInternalServiceTokenProvider returning the configured Keycloak:ServiceClientAccessToken (Development only).");
        return Task.FromResult(_staticToken);
    }
}

internal static class InternalServiceTokenProviderRegistration
{
    public static IServiceCollection AddInternalServiceTokenProvider(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var staticToken = configuration["Keycloak:ServiceClientAccessToken"];
        if (!string.IsNullOrWhiteSpace(staticToken))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    $"Keycloak:ServiceClientAccessToken must not be set in environment '{environment.EnvironmentName}'. Use the KeycloakClientCredentialsTokenProvider production adapter instead.");
            }
            services.AddSingleton<IInternalServiceTokenProvider>(new StaticInternalServiceTokenProvider(configuration, environment, LoggerFactory.Create(b => b.AddConsole()).CreateLogger<StaticInternalServiceTokenProvider>()));
        }
        else
        {
            services.AddHttpClient<IInternalServiceTokenProvider, KeycloakClientCredentialsTokenProvider>();
        }
        return services;
    }
}
