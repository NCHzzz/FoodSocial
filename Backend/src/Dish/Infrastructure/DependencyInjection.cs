using FoodSocial.Dish.Application;
using FoodSocial.Dish.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Dish.Infrastructure;

/// <summary>
/// Registers Dish infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Dish infrastructure services.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("dishdb")
            ?? throw new InvalidOperationException("Connection string 'dishdb' is not configured.");

        services.AddDbContext<DishDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__DishMigrationsHistory")));

        services.AddScoped<IDishStore, DishStore>();

        return services;
    }
}
