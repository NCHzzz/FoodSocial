namespace FoodSocial.ApiGateway;

internal sealed record GatewayOptions(
    Uri BffAddress,
    int ProxyActivityTimeoutSeconds,
    int BffHealthTimeoutSeconds,
    long MaxRequestBodySizeBytes)
{
    public static GatewayOptions Load(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var bffAddress = ResolveBffAddress(configuration);
        var proxyActivityTimeoutSeconds = Math.Max(
            1,
            configuration.GetValue("Gateway:Proxy:ActivityTimeoutSeconds", 30));
        var bffHealthTimeoutSeconds = Math.Max(
            1,
            configuration.GetValue("Gateway:Health:BffTimeoutSeconds", 3));
        var maxRequestBodySizeBytes = Math.Max(
            1024 * 1024,
            configuration.GetValue<long?>("Gateway:MaxRequestBodySizeBytes")
                ?? 25L * 1024L * 1024L);

        return new GatewayOptions(
            bffAddress,
            proxyActivityTimeoutSeconds,
            bffHealthTimeoutSeconds,
            maxRequestBodySizeBytes);
    }

    private static Uri ResolveBffAddress(IConfiguration configuration)
    {
        var discoveredAddress = FirstConfigured(
            configuration["Services:bff:http:0"],
            configuration["Services:bff:https:0"],
            configuration["Gateway:Clusters:Bff:Address"])
            ?? throw new InvalidOperationException(
                "BFF cluster address is not configured. Set Gateway:Clusters:Bff:Address or provide an Aspire service reference for 'bff'.");

        return NormalizeBaseAddress(discoveredAddress);
    }

    private static string? FirstConfigured(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static Uri NormalizeBaseAddress(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Gateway BFF cluster address must be an absolute HTTP or HTTPS URI.");
        }

        var normalized = uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? uri.AbsoluteUri
            : $"{uri.AbsoluteUri}/";

        return new Uri(normalized, UriKind.Absolute);
    }
}
