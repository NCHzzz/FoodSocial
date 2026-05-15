using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace FoodSocial.ApiGateway;

internal static class GatewayProxyConfiguration
{
    private const string BffClusterId = "bff";

    public static IReadOnlyList<RouteConfig> CreateRoutes() =>
    [
        CreateBffRoute("bff-api-root", "/api"),
        CreateBffRoute("bff-api", "/api/{**catch-all}"),
        CreateBffRoute("bff-signin-callback", "/signin-oidc"),
        CreateBffRoute("bff-signout-callback", "/signout-callback-oidc")
    ];

    public static IReadOnlyList<ClusterConfig> CreateClusters(GatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return
        [
            new ClusterConfig
            {
                ClusterId = BffClusterId,
                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
                {
                    ["primary"] = new DestinationConfig
                    {
                        Address = options.BffAddress.AbsoluteUri
                    }
                },
                HttpRequest = new ForwarderRequestConfig
                {
                    ActivityTimeout = TimeSpan.FromSeconds(options.ProxyActivityTimeoutSeconds)
                }
            }
        ];
    }

    private static RouteConfig CreateBffRoute(string routeId, string path) =>
        new()
        {
            RouteId = routeId,
            ClusterId = BffClusterId,
            Match = new RouteMatch
            {
                Path = path
            }
        };
}
