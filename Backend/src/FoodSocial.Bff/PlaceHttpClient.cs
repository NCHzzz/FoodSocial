using System.Net.Http.Headers;

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

    private static bool RequiresBody(string method) =>
        HttpMethods.IsPost(method)
        || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method);
}
