using FoodSocial.Messaging;
using FoodSocial.Place.Api.Health;
using FoodSocial.Place.Application.DependencyInjection;
using FoodSocial.Place.Infrastructure.DependencyInjection;
using FoodSocial.Place.Infrastructure.Persistence;

namespace FoodSocial.Place.Api.DependencyInjection;

/// <summary>
/// Registers Place API services.
/// </summary>
public static class PlaceApiServiceRegistration
{
    /// <summary>
    /// Adds Place API startup services.
    /// </summary>
    public static IServiceCollection AddPlaceApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddScoped<PlaceManagementAuthorizationFilter>();

        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddHealthChecks()
            .AddCheck<PlacePostgresHealthCheck>("place-postgres")
            .AddCheck<RabbitMqHealthCheck>("place-rabbitmq");
        services.AddHostedService<IntegrationOutboxPublisherBackgroundService<PlaceDbContext>>();

        return services;
    }
}
