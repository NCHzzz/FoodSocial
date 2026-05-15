using FoodSocial.Dish.Application.DependencyInjection;
using FoodSocial.Dish.Infrastructure.DependencyInjection;
using FoodSocial.Dish.Infrastructure.Persistence;
using FoodSocial.Messaging;

namespace FoodSocial.Dish.Api.DependencyInjection;

/// <summary>
/// Registers Dish API services.
/// </summary>
public static class DishApiServiceRegistration
{
    /// <summary>
    /// Adds Dish API startup services.
    /// </summary>
    public static IServiceCollection AddDishApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddControllers();

        services.AddOpenApi();

        services.AddProblemDetails();
        services.AddScoped<DishManagementAuthorizationFilter>();

        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddHealthChecks()
            .AddCheck<DishPostgresHealthCheck>("dish-postgres")
            .AddCheck<RabbitMqHealthCheck>("dish-rabbitmq");
        services.AddHostedService<IntegrationOutboxPublisherBackgroundService<DishDbContext>>();

        return services;
    }
}
