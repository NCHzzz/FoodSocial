using FoodSocial.Dish.Application.Interfaces.Repositories;
using FoodSocial.Dish.Infrastructure.Persistence;
using FoodSocial.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Dish.Infrastructure.DependencyInjection;

/// <summary>
/// Registers Dish infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
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
        services.AddScoped<IntegrationOutboxDrainWorker<DishDbContext>>();
        AddRabbitMqPublisher(services, configuration, "Dish");

        return services;
    }

    private static void AddRabbitMqPublisher(IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var rabbitMqConnectionString = configuration.GetConnectionString("rabbitmq")
            ?? configuration["ConnectionStrings:rabbitmq"]
            ?? configuration["RabbitMQ:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(rabbitMqConnectionString))
        {
            services.AddSingleton<IIntegrationEventPublisher>(_ => new RabbitMqIntegrationEventPublisher(rabbitMqConnectionString));
        }
        else
        {
            services.AddSingleton<IIntegrationEventPublisher>(_ => new MissingRabbitMqConfigurationIntegrationEventPublisher(serviceName));
        }
    }
}
