using FoodSocial.Post.Application;
using FoodSocial.Post.Infrastructure.Persistence;
using FoodSocial.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Post.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services); ArgumentNullException.ThrowIfNull(configuration);
        var connectionString = configuration.GetConnectionString("postdb") ?? throw new InvalidOperationException("Connection string 'postdb' is not configured.");
        services.AddDbContext<PostDbContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__PostMigrationsHistory")));
        services.AddScoped<IPostStore, PostStore>();
        services.AddScoped<IntegrationOutboxDrainWorker<PostDbContext>>();
        AddRabbitMqPublisher(services, configuration, "Post");
        services.AddScoped<IPostCatalogReferenceValidator, HttpPostCatalogReferenceValidator>();
        services.AddHttpClient<DishCatalogHttpClient>((serviceProvider, client) =>
        {
            client.BaseAddress = CatalogHttpClientConfiguration.ResolveDishApiBaseAddress(serviceProvider.GetRequiredService<IConfiguration>());
            client.Timeout = TimeSpan.FromSeconds(5);
        })
        .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var baseAddress = CatalogHttpClientConfiguration.ResolveDishApiBaseAddress(configuration);
            return ShouldAllowInsecureLocalBackchannel(baseAddress)
                ? new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }
                : new HttpClientHandler();
        });
        services.AddHttpClient<PlaceCatalogHttpClient>((serviceProvider, client) =>
        {
            client.BaseAddress = CatalogHttpClientConfiguration.ResolvePlaceApiBaseAddress(serviceProvider.GetRequiredService<IConfiguration>());
            client.Timeout = TimeSpan.FromSeconds(5);
        })
        .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var baseAddress = CatalogHttpClientConfiguration.ResolvePlaceApiBaseAddress(configuration);
            return ShouldAllowInsecureLocalBackchannel(baseAddress)
                ? new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }
                : new HttpClientHandler();
        });
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

    private static bool ShouldAllowInsecureLocalBackchannel(Uri serviceBaseAddress) =>
        serviceBaseAddress.Scheme == Uri.UriSchemeHttps
        && string.Equals(serviceBaseAddress.Host, "localhost", StringComparison.OrdinalIgnoreCase);
}
