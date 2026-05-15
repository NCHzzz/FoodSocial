using FoodSocial.Messaging;
using FoodSocial.UserProfile.Api.Health;
using FoodSocial.UserProfile.Application.DependencyInjection;
using FoodSocial.UserProfile.Infrastructure.DependencyInjection;
using FoodSocial.UserProfile.Infrastructure.Messaging;
using FoodSocial.UserProfile.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace FoodSocial.UserProfile.Api.DependencyInjection;

/// <summary>
/// Registers UserProfile API services.
/// </summary>
public static class UserProfileApiServiceRegistration
{
    /// <summary>
    /// Adds UserProfile API startup services.
    /// </summary>
    public static IServiceCollection AddUserProfileApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var audience = configuration["Keycloak:Audience"];
        var realm = configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak realm is not configured.");
        var keycloakAuthority = KeycloakStartup.ResolveKeycloakAuthority(configuration, realm);
        var requireHttpsMetadata = KeycloakStartup.ResolveRequireHttpsMetadata(configuration, environment, keycloakAuthority);
        var allowInsecureDevBackchannel = KeycloakStartup.ShouldAllowInsecureDevBackchannel(environment, keycloakAuthority);

        services.AddControllers();
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddOpenApi();

        services.AddProblemDetails();

        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddHealthChecks()
            .AddCheck<UserProfilePostgresHealthCheck>("userprofile-postgres")
            .AddCheck<RabbitMqHealthCheck>("userprofile-rabbitmq");
        services.AddHostedService<UserProvisionedConsumerBackgroundService>();
        services.AddHostedService<IntegrationOutboxPublisherBackgroundService<UserProfileDbContext>>();

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
                options.Audience = audience;
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
