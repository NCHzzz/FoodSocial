namespace FoodSocial.Bff;

internal static class BffCatalogCacheExtensions
{
    public static IServiceCollection AddBffCatalogResponseCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<BffCatalogCacheOptions>(configuration.GetSection("CatalogCache"));

        var redisConnectionString = configuration.GetConnectionString("redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "FoodSocial:Bff:";
            });
        }

        services.AddSingleton<BffCatalogResponseCache>();

        return services;
    }
}
