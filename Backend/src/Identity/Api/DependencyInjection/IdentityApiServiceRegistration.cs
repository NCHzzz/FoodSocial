using FoodSocial.Identity.Api.Authorization;
using FoodSocial.Identity.Api.Health;
using FoodSocial.Identity.Application.DependencyInjection;
using FoodSocial.Identity.Domain;
using FoodSocial.Identity.Infrastructure.DependencyInjection;
using FoodSocial.Identity.Infrastructure.Messaging;
using FoodSocial.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace FoodSocial.Identity.Api.DependencyInjection;

/// <summary>
/// Registers Identity API services.
/// </summary>
public static class IdentityApiServiceRegistration
{
    /// <summary>
    /// Adds Identity API startup services.
    /// </summary>
    public static IServiceCollection AddIdentityApiServices(
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

        services.AddOpenApi();

        services.AddProblemDetails();

        services.AddApplication();

        services.AddInfrastructure(configuration);
        services.AddHealthChecks()
            .AddCheck<IdentityPostgresHealthCheck>("identity-postgres")
            .AddCheck<RabbitMqHealthCheck>("identity-rabbitmq");
        services.AddHostedService<IdentityOutboxPublisherBackgroundService>();
        services.AddHostedService<UserProfileBootstrapCompletedConsumerBackgroundService>();

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

        services.AddHttpContextAccessor();
        services.AddScoped<IAuthorizationHandler, IdentityAppRoleAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, TrustedInternalServiceAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(
                IdentityAuthorizationPolicies.AccountAdminOnly,
                policy => policy.Requirements.Add(
                    new IdentityAppRoleRequirement(new HashSet<string>(StringComparer.Ordinal)
                    {
                        IdentityAppRoles.Admin
                    })));

            options.AddPolicy(
                IdentityAuthorizationPolicies.AccountAdminOrModerator,
                policy => policy.Requirements.Add(
                    new IdentityAppRoleRequirement(new HashSet<string>(StringComparer.Ordinal)
                    {
                        IdentityAppRoles.Admin,
                        IdentityAppRoles.Moderator
                    })));

            options.AddPolicy(
                IdentityAuthorizationPolicies.TrustedInternalService,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new TrustedInternalServiceRequirement());
                });
        });

        return services;
    }
}
