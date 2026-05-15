using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Place.Application.DependencyInjection;

/// <summary>
/// Registers Place application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Place application services.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<PlaceCatalogService>();

        return services;
    }
}
