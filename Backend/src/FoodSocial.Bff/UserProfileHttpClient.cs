using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace FoodSocial.Bff;

internal sealed class UserProfileHttpClient(HttpClient httpClient, ILogger<UserProfileHttpClient> logger)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<UserProfileHttpClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<HttpResponseMessage> ForwardAsync(
        HttpRequest sourceRequest,
        string targetPathAndQuery,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);

        using var request = new HttpRequestMessage(new HttpMethod(sourceRequest.Method), targetPathAndQuery);
        if (RequiresBody(sourceRequest.Method))
        {
            request.Content = new StreamContent(sourceRequest.Body);
            if (!string.IsNullOrWhiteSpace(sourceRequest.ContentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(sourceRequest.ContentType);
            }
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "UserProfile request to {TargetPathAndQuery} timed out after {Timeout}.",
                targetPathAndQuery,
                _httpClient.Timeout);

            throw new InvalidOperationException(
                $"UserProfile request to '{targetPathAndQuery}' timed out after {_httpClient.Timeout}.",
                exception);
        }

        return response;
    }

    public async Task<HttpResponseMessage> GetAsync(
        string targetPathAndQuery,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);

        using var request = new HttpRequestMessage(HttpMethod.Get, targetPathAndQuery);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        try
        {
            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "UserProfile request to {TargetPathAndQuery} timed out after {Timeout}.",
                targetPathAndQuery,
                _httpClient.Timeout);

            throw new InvalidOperationException(
                $"UserProfile request to '{targetPathAndQuery}' timed out after {_httpClient.Timeout}.",
                exception);
        }
    }

    public static async Task<string?> ResolveAccessTokenAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var accessToken = await httpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return accessToken;
        }

        return BffRequestConventions.ResolveBearerTokenOrNull(httpContext.Request);
    }

    private static bool RequiresBody(string method) =>
        HttpMethods.IsPost(method)
        || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method);
}
