using System.Net.Http.Headers;
using System.Net.Http.Json;
using FoodSocial.Contracts.Places;

namespace FoodSocial.Bff;

internal sealed class PlaceHttpClient(HttpClient httpClient, ILogger<PlaceHttpClient> logger)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<PlaceHttpClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<HttpResponseMessage> ForwardAsync(
        HttpRequest sourceRequest,
        string targetPathAndQuery,
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

        try
        {
            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "Place request to {TargetPathAndQuery} timed out after {Timeout}.",
                targetPathAndQuery,
                _httpClient.Timeout);

            throw new InvalidOperationException(
                $"Place request to '{targetPathAndQuery}' timed out after {_httpClient.Timeout}.",
                exception);
        }
    }


    public async Task<PlaceSearchResponse?> SearchAsync(string targetPathAndQuery, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);

        using var request = new HttpRequestMessage(HttpMethod.Get, targetPathAndQuery);

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Place search request to {TargetPathAndQuery} returned {StatusCode} for recommendation fallback.",
                    targetPathAndQuery,
                    (int)response.StatusCode);

                return null;
            }

            return await response.Content.ReadFromJsonAsync<PlaceSearchResponse>(cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "Place search request to {TargetPathAndQuery} timed out after {Timeout}.",
                targetPathAndQuery,
                _httpClient.Timeout);

            throw new InvalidOperationException(
                $"Place search request to '{targetPathAndQuery}' timed out after {_httpClient.Timeout}.",
                exception);
        }
    }

    private static bool RequiresBody(string method) =>
        HttpMethods.IsPost(method)
        || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method);
}
