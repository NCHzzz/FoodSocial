using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Dish.Application;

/// <summary>
/// Registers Dish application services.
/// </summary>
public static class DependencyInjection
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
