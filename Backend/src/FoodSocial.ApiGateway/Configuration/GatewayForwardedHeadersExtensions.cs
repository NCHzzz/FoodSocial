using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using NetworkRange = System.Net.IPNetwork;

namespace FoodSocial.ApiGateway;

internal static class GatewayForwardedHeadersExtensions
{
    public static IServiceCollection ConfigureGatewayForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var knownProxyValues = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
        var knownNetworkValues = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
        var knownProxies = ParseKnownProxies(knownProxyValues);
        var knownNetworks = ParseKnownNetworks(knownNetworkValues);

        if (!environment.IsDevelopment() && knownProxies.Count == 0 && knownNetworks.Count == 0)
        {
            throw new InvalidOperationException("Gateway forwarded headers trusted proxies or networks must be configured outside Development.");
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;
            options.ForwardLimit = 1;
            options.RequireHeaderSymmetry = true;

            foreach (var proxy in knownProxies)
            {
                options.KnownProxies.Add(proxy);
            }

            foreach (var network in knownNetworks)
            {
                options.KnownIPNetworks.Add(network);
            }
        });

        return services;
    }

    private static IReadOnlyList<IPAddress> ParseKnownProxies(IEnumerable<string> values)
    {
        var proxies = new List<IPAddress>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!IPAddress.TryParse(value, out var proxy))
            {
                throw new InvalidOperationException($"Gateway forwarded headers known proxy '{value}' must be a valid IP address.");
            }

            proxies.Add(proxy);
        }

        return proxies;
    }

    private static IReadOnlyList<NetworkRange> ParseKnownNetworks(IEnumerable<string> values)
    {
        var networks = new List<NetworkRange>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!NetworkRange.TryParse(value, out var network))
            {
                throw new InvalidOperationException($"Gateway forwarded headers known network '{value}' must be a valid CIDR network.");
            }

            networks.Add(network);
        }

        return networks;
    }
}
