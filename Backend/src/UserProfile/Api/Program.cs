using FoodSocial.Messaging;
using FoodSocial.UserProfile.Api.Health;
using FoodSocial.UserProfile.Application;
using FoodSocial.UserProfile.Infrastructure;
using FoodSocial.UserProfile.Infrastructure.Messaging;
using FoodSocial.UserProfile.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

var audience = builder.Configuration["Keycloak:Audience"];
var realm = builder.Configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak realm is not configured.");
var keycloakAuthority = ResolveKeycloakAuthority(builder.Configuration, realm);
var requireHttpsMetadata = ResolveRequireHttpsMetadata(builder.Configuration, builder.Environment, keycloakAuthority);
var allowInsecureDevBackchannel = ShouldAllowInsecureDevBackchannel(builder.Environment, keycloakAuthority);

Microsoft.Extensions.Hosting.ServiceDefaultsExtensions.AddServiceDefaults(builder);

builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<UserProfilePostgresHealthCheck>("userprofile-postgres")
    .AddCheck<RabbitMqHealthCheck>("userprofile-rabbitmq");
builder.Services.AddHostedService<UserProvisionedConsumerBackgroundService>();

builder.Services
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
            options.BackchannelHttpHandler = CreateDevelopmentBackchannelHandler();
        }
    });

var app = builder.Build();

var applyMigrationsOnStartup = ShouldApplyMigrationsOnStartup(builder.Configuration, builder.Environment, requireExplicitNonDevelopmentFlag: true);

if (applyMigrationsOnStartup)
{
    await ApplyUserProfileDatabaseMigrationsAsync(app.Services);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet(
        "/_diagnostics/userprofile-startup",
        (IConfiguration configuration, UserProfileDbContext dbContext) =>
        {
            var connectionString = dbContext.Database.GetConnectionString();
            var connectionBuilder = string.IsNullOrWhiteSpace(connectionString)
                ? null
                : new NpgsqlConnectionStringBuilder(connectionString);

            return Results.Ok(new
            {
                ApplyMigrationsOnStartup = ShouldApplyMigrationsOnStartup(configuration, app.Environment, requireExplicitNonDevelopmentFlag: true),
                Provider = dbContext.Database.ProviderName,
                Database = connectionBuilder?.Database,
                Host = connectionBuilder?.Host,
                Port = connectionBuilder?.Port
            });
        });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

Microsoft.Extensions.Hosting.ServiceDefaultsExtensions.MapDefaultEndpoints(app);

app.Run();

static async Task ApplyUserProfileDatabaseMigrationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("FoodSocial.UserProfile.Api.DatabaseInitialization");
    var connectionString = dbContext.Database.GetConnectionString()
        ?? throw new InvalidOperationException("UserProfile database connection string is not configured.");
    var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);

    logger.LogInformation(
        "Applying UserProfile EF migrations against database {DatabaseName} on host {Host}:{Port}.",
        connectionBuilder.Database,
        connectionBuilder.Host,
        connectionBuilder.Port);

    await dbContext.Database.MigrateAsync();

    logger.LogInformation(
        "Finished applying UserProfile EF migrations against database {DatabaseName}.",
        connectionBuilder.Database);
}

static bool ShouldApplyMigrationsOnStartup(
    IConfiguration configuration,
    IHostEnvironment environment,
    bool requireExplicitNonDevelopmentFlag = false)
{
    var configuredValue = configuration.GetValue<bool?>("UserProfile:ApplyMigrationsOnStartup");
    if (configuredValue is not null)
    {
        return configuredValue.Value;
    }

    if (!environment.IsDevelopment() && requireExplicitNonDevelopmentFlag)
    {
        throw new InvalidOperationException("UserProfile:ApplyMigrationsOnStartup must be explicitly configured outside development.");
    }

    return environment.IsDevelopment();
}

static string ResolveKeycloakAuthority(IConfiguration configuration, string realm)
{
    var configuredAuthority = configuration["Keycloak:Authority"];
    if (!string.IsNullOrWhiteSpace(configuredAuthority))
    {
        return configuredAuthority;
    }

    var discoveredAuthority = configuration["Services:keycloak:https:0"]
        ?? configuration["Services:keycloak:http:0"]
        ?? throw new InvalidOperationException(
            "Keycloak authority is not configured. Set Keycloak:Authority or provide an Aspire service reference for 'keycloak'.");

    return $"{discoveredAuthority.TrimEnd('/')}/realms/{realm}";
}

static bool ResolveRequireHttpsMetadata(IConfiguration configuration, IHostEnvironment environment, string keycloakAuthority)
{
    var configuredRequirement = configuration.GetValue<bool?>("Authentication:RequireHttpsMetadata")
        ?? !environment.IsDevelopment();

    if (!Uri.TryCreate(keycloakAuthority, UriKind.Absolute, out var authorityUri))
    {
        return configuredRequirement;
    }

    return configuredRequirement && authorityUri.Scheme == Uri.UriSchemeHttps;
}

static bool ShouldAllowInsecureDevBackchannel(IHostEnvironment environment, string keycloakAuthority)
{
    if (!environment.IsDevelopment())
    {
        return false;
    }

    return Uri.TryCreate(keycloakAuthority, UriKind.Absolute, out var authorityUri)
        && authorityUri.Scheme == Uri.UriSchemeHttps
        && string.Equals(authorityUri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
}

static HttpMessageHandler CreateDevelopmentBackchannelHandler() =>
    new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

public partial class Program;
