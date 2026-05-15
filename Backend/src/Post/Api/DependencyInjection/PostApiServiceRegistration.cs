using FoodSocial.Messaging;
using FoodSocial.Post.Application.DependencyInjection;
using FoodSocial.Post.Infrastructure.DependencyInjection;
using FoodSocial.Post.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace FoodSocial.Post.Api.DependencyInjection;

/// <summary>
/// Registers Post API services.
/// </summary>
public static class PostApiServiceRegistration
{
    /// <summary>
    /// Adds Post API startup services.
    /// </summary>
    public static IServiceCollection AddPostApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var realm = configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak realm is not configured.");
        var keycloakAuthority = KeycloakStartup.ResolveKeycloakAuthority(configuration, realm);
        var requireHttpsMetadata = KeycloakStartup.ResolveRequireHttpsMetadata(configuration, environment, keycloakAuthority);
        var allowInsecureDevBackchannel = KeycloakStartup.ShouldAllowInsecureDevBackchannel(environment, keycloakAuthority);

        services.AddControllers();

        services.AddOpenApi();

        services.AddProblemDetails();
        services.AddScoped<PostInternalAuthorizationFilter>();
        services.AddScoped<PostModeratorAuthorizationFilter>();
        services.AddSingleton<PostMediaStorage>();
        services.AddSingleton<PostMediaUploadValidator>();
        services.AddScoped<PostMediaUploadWorkflow>();
        services.AddSingleton<PostApiMetrics>();

        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddHealthChecks()
            .AddCheck<PostPostgresHealthCheck>("post-postgres")
            .AddCheck<RabbitMqHealthCheck>("post-rabbitmq");
        services.AddHostedService<IntegrationOutboxPublisherBackgroundService<PostDbContext>>();
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = keycloakAuthority;
                options.MetadataAddress = $"{keycloakAuthority.TrimEnd('/')}/.well-known/openid-configuration";
                options.Audience = configuration["Keycloak:Audience"];
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.MapInboundClaims = false;
                if (allowInsecureDevBackchannel)
                {
                    options.BackchannelHttpHandler = KeycloakStartup.CreateDevelopmentBackchannelHandler();
                }
            });

        return services;
    }
}
