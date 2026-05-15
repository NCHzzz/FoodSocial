using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FoodSocial.Contracts.Identity;
using Microsoft.Extensions.Logging;

namespace FoodSocial.Bff;

internal sealed class IdentityOrchestrationHttpClient(
    HttpClient httpClient,
    IInternalServiceTokenProvider serviceTokenProvider,
    ILogger<IdentityOrchestrationHttpClient> logger)
{
    private const string PublicEnsurePath = "/identity/current-user/ensure";
    private const string InternalEnsurePath = "/identity/current-user/internal/ensure";

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IInternalServiceTokenProvider _serviceTokenProvider = serviceTokenProvider ?? throw new ArgumentNullException(nameof(serviceTokenProvider));
    private readonly ILogger<IdentityOrchestrationHttpClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<EnsureIdentityResponse> EnsureIdentityAsync(
        ClaimsPrincipal user,
        string? accessToken,
        bool useTrustedInternalEnsure,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var subject = IdentityClaimReader.GetSubject(user);
        var preferredUsername = IdentityClaimReader.GetPreferredUsername(user);
        var email = IdentityClaimReader.GetEmail(user);

        if (string.IsNullOrWhiteSpace(subject) && !string.IsNullOrWhiteSpace(accessToken))
        {
            var accessTokenClaims = TryReadAccessTokenPayloadClaims(accessToken);
            subject = accessTokenClaims?.Subject;
            preferredUsername ??= accessTokenClaims?.PreferredUsername;
            email ??= accessTokenClaims?.Email;
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            var availableClaimTypes = string.Join(
                ", ",
                user.Claims
                    .Select(claim => claim.Type)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(type => type, StringComparer.OrdinalIgnoreCase));

            throw new InvalidOperationException($"The authenticated principal did not contain a subject claim. Available claim types: {availableClaimTypes}");
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("The authenticated session did not contain an access token for trusted identity orchestration.");
        }

        var requestPath = useTrustedInternalEnsure ? InternalEnsurePath : PublicEnsurePath;
        object requestBody = useTrustedInternalEnsure
            ? new TrustedEnsureIdentityRequest(subject, preferredUsername, email)
            : new EnsureIdentityRequest();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            requestPath)
        {
            Content = JsonContent.Create(requestBody)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            useTrustedInternalEnsure
                ? await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken)
                : accessToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "Identity ensure request to {RequestPath} timed out after {Timeout}.",
                requestPath,
                _httpClient.Timeout);

            throw new InvalidOperationException(
                $"Identity ensure request to '{requestPath}' timed out after {_httpClient.Timeout}.",
                exception);
        }

        using var responseScope = response;
        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<EnsureIdentityResponse>(cancellationToken);
            return payload ?? throw new InvalidOperationException("The Identity API ensure response payload was empty.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Identity ensure request to {RequestPath} returned {StatusCode} {ReasonPhrase}. Response body: {ResponseBody}",
            requestPath,
            (int)response.StatusCode,
            response.ReasonPhrase,
            responseBody);

        throw new InvalidOperationException(
            $"Identity ensure orchestration failed with {(int)response.StatusCode} {response.ReasonPhrase}.");
    }

    private AccessTokenPayloadClaims? TryReadAccessTokenPayloadClaims(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var segments = accessToken.Split('.');
        if (segments.Length < 2)
        {
            return null;
        }

        var payloadSegment = segments[1]
            .Replace('-', '+')
            .Replace('_', '/');
        payloadSegment = payloadSegment.PadRight(payloadSegment.Length + ((4 - payloadSegment.Length % 4) % 4), '=');

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(payloadSegment));
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;

            return new AccessTokenPayloadClaims(
                GetStringProperty(root, IdentityClaims.Subject),
                GetStringProperty(root, IdentityClaims.PreferredUsername),
                GetStringProperty(root, IdentityClaims.Email));
        }
        catch (FormatException exception)
        {
            _logger.LogWarning(exception, "The BFF could not read claims from the access token payload.");
            return null;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "The BFF could not read claims from the access token payload.");
            return null;
        }
    }

    private static string? GetStringProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var propertyValue) && propertyValue.ValueKind == JsonValueKind.String
            ? propertyValue.GetString()
            : null;

    private sealed record AccessTokenPayloadClaims(string? Subject, string? PreferredUsername, string? Email);
}
