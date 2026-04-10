using Aspire.Hosting;
using Aspire.Hosting.Testing;
using System.Net.Http;

namespace FoodSocial.AppHost.Tests;

internal sealed class AppHostResourceDiscovery(DistributedApplication app)
{
    private readonly DistributedApplication _app = app ?? throw new ArgumentNullException(nameof(app));

    public HttpClient CreateHttpClient(string resourceName, string? endpointName = null)
    {
        var endpoint = GetEndpoint(resourceName, endpointName);
        var handler = ShouldBypassCertificateValidation(endpoint)
            ? CreateDevelopmentHttpsHandler()
            : new HttpClientHandler();

        return new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = endpoint
        };
    }

    public Uri GetEndpoint(string resourceName, string? endpointName = null) =>
        _app.GetEndpoint(resourceName, endpointName);

    private static bool ShouldBypassCertificateValidation(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return endpoint.Scheme == Uri.UriSchemeHttps
            && string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClientHandler CreateDevelopmentHttpsHandler() =>
        new()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
}
