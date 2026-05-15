using FoodSocial.Place.Application.Interfaces.Repositories;
using FoodSocial.Place.Infrastructure.Persistence;
using FoodSocial.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Place.Infrastructure.DependencyInjection;

/// <summary>
/// Registers Place infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
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
        services.AddScoped<IntegrationOutboxDrainWorker<PlaceDbContext>>();
        AddRabbitMqPublisher(services, configuration, "Place");

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
