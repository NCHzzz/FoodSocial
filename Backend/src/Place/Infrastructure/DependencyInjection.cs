using FoodSocial.Place.Application;
using FoodSocial.Place.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Place.Infrastructure;

/// <summary>
/// Registers Place infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Place infrastructure services.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("placedb")
            ?? throw new InvalidOperationException("Connection string 'placedb' is not configured.");

        services.AddDbContext<PlaceDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__PlaceMigrationsHistory")));

        services.AddScoped<IPlaceStore, PlaceStore>();

        return services;
    }
}
