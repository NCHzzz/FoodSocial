using FoodSocial.Identity.Application.Interfaces.Repositories;
using FoodSocial.Identity.Infrastructure.Messaging;
using FoodSocial.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Identity.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("identitydb")
            ?? throw new InvalidOperationException("Connection string 'identitydb' is not configured.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__IdentityMigrationsHistory")));

        services.AddScoped<IIdentityUserStore, IdentityUserStore>();
        services.AddScoped<IdentityOutboxDrainWorker>();

        var rabbitMqConnectionString =
            configuration.GetConnectionString("rabbitmq")
            ?? configuration["ConnectionStrings:rabbitmq"]
            ?? configuration["RabbitMQ:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(rabbitMqConnectionString))
        {
            services.AddSingleton<IIdentityIntegrationEventPublisher>(_ => new RabbitMqIdentityIntegrationEventPublisher(rabbitMqConnectionString));
        }
        else
        {
            services.AddSingleton<IIdentityIntegrationEventPublisher, MissingRabbitMqConfigurationIdentityIntegrationEventPublisher>();
        }

        return services;
    }
}
