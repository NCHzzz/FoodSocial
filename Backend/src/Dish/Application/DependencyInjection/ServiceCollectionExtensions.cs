using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Dish.Application.DependencyInjection;

/// <summary>
/// Registers Dish application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Dish application services.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<DishCatalogService>();

        return services;
    }
}
