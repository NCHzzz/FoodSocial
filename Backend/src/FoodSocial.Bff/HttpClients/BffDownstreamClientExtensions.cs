namespace FoodSocial.Bff;

internal static class BffDownstreamClientExtensions
{
    public static IServiceCollection AddBffIdentityOrchestrationClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddBffDownstreamClient<IdentityOrchestrationHttpClient>(ResolveIdentityApiBaseAddress);
    }

    public static IServiceCollection AddBffUserProfileClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddBffDownstreamClient<UserProfileHttpClient>(ResolveUserProfileApiBaseAddress);
    }

    public static IServiceCollection AddBffDishClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddBffDownstreamClient<DishHttpClient>(ResolveDishApiBaseAddress);
    }

    public static IServiceCollection AddBffPlaceClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddBffDownstreamClient<PlaceHttpClient>(ResolvePlaceApiBaseAddress);
    }

    private static IServiceCollection AddBffDownstreamClient<TClient>(
        this IServiceCollection services,
        Func<IConfiguration, Uri> resolveBaseAddress)
        where TClient : class
    {
        services
            .AddHttpClient<TClient>((serviceProvider, client) =>
            {
                client.BaseAddress = resolveBaseAddress(serviceProvider.GetRequiredService<IConfiguration>());
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                var serviceBaseAddress = resolveBaseAddress(configuration);

                return ShouldAllowInsecureDevServiceBackchannel(environment, serviceBaseAddress)
                    ? CreateDevelopmentBackchannelHandler()
                    : new HttpClientHandler();
            });

        return services;
    }

    private static Uri ResolveIdentityApiBaseAddress(IConfiguration configuration) =>
        ResolveServiceBaseAddress(configuration, "identity-api", "Identity API");

    private static Uri ResolveUserProfileApiBaseAddress(IConfiguration configuration) =>
        ResolveServiceBaseAddress(configuration, "userprofile-api", "UserProfile API");

    private static Uri ResolveDishApiBaseAddress(IConfiguration configuration) =>
        ResolveServiceBaseAddress(configuration, "dish-api", "Dish API");

    private static Uri ResolvePlaceApiBaseAddress(IConfiguration configuration) =>
        ResolveServiceBaseAddress(configuration, "place-api", "Place API");

    private static Uri ResolveServiceBaseAddress(IConfiguration configuration, string serviceName, string displayName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var discoveredAddress = configuration[$"Services:{serviceName}:https:0"]
            ?? configuration[$"Services:{serviceName}:http:0"]
            ?? throw new InvalidOperationException(
                $"{displayName} base address is not configured. Set an Aspire service reference for '{serviceName}'.");

        return new Uri(discoveredAddress.TrimEnd('/'), UriKind.Absolute);
    }

    private static bool ShouldAllowInsecureDevServiceBackchannel(IHostEnvironment environment, Uri serviceBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(serviceBaseAddress);

        return environment.IsDevelopment()
            && serviceBaseAddress.Scheme == Uri.UriSchemeHttps
            && string.Equals(serviceBaseAddress.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpMessageHandler CreateDevelopmentBackchannelHandler() =>
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
}
