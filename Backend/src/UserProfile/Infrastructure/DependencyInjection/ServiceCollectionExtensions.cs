using FoodSocial.UserProfile.Application.Interfaces.Repositories;
using FoodSocial.UserProfile.Infrastructure.Messaging;
using FoodSocial.UserProfile.Infrastructure.Persistence;
using FoodSocial.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.UserProfile.Infrastructure.DependencyInjection;

/// <summary>
/// Registers UserProfile infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds UserProfile infrastructure services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("userprofiledb")
            ?? throw new InvalidOperationException("Connection string 'userprofiledb' is not configured.");

        services.AddDbContext<UserProfileDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__UserProfileMigrationsHistory")));

        services.AddScoped<IUserProfileShellStore, UserProfileShellStore>();
        services.AddScoped<IUserProfileStore, UserProfileStore>();
        services.AddScoped<IntegrationOutboxDrainWorker<UserProfileDbContext>>();

        var rabbitMqConnectionString = configuration.GetConnectionString("rabbitmq")
            ?? configuration["ConnectionStrings:rabbitmq"]
            ?? configuration["RabbitMQ:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(rabbitMqConnectionString))
        {
            services.AddSingleton<IUserProfileBootstrapCompletionPublisher>(_ => new RabbitMqUserProfileBootstrapCompletionPublisher(rabbitMqConnectionString));
            services.AddSingleton<IIntegrationEventPublisher>(_ => new RabbitMqIntegrationEventPublisher(rabbitMqConnectionString));
        }
        else
        {
            services.AddSingleton<IUserProfileBootstrapCompletionPublisher, MissingRabbitMqConfigurationUserProfileBootstrapCompletionPublisher>();
            services.AddSingleton<IIntegrationEventPublisher>(_ => new MissingRabbitMqConfigurationIntegrationEventPublisher("UserProfile"));
        }

        return services;
    }
}
