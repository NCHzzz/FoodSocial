using FoodSocial.Identity.Application;
using FoodSocial.Identity.Api.Authorization;
using FoodSocial.Identity.Api.Health;
using FoodSocial.Identity.Domain;
using FoodSocial.Identity.Infrastructure;
using FoodSocial.Identity.Infrastructure.Messaging;
using FoodSocial.Identity.Infrastructure.Persistence;
using FoodSocial.Messaging;
using Microsoft.AspNetCore.Authorization;
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

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<IdentityPostgresHealthCheck>("identity-postgres")
    .AddCheck<RabbitMqHealthCheck>("identity-rabbitmq");
builder.Services.AddHostedService<IdentityOutboxPublisherBackgroundService>();
builder.Services.AddHostedService<UserProfileBootstrapCompletedConsumerBackgroundService>();

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, IdentityAppRoleAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, TrustedInternalServiceAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
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

var app = builder.Build();

if (builder.Configuration.GetValue("Identity:ApplyMigrationsOnStartup", true))
{
    await EnsureDatabaseExistsAsync(app.Services, "identitydb");
    await ApplyIdentityDatabaseMigrationsAsync(app.Services);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();

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

static async Task ApplyIdentityDatabaseMigrationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.MigrateAsync();
}

static async Task EnsureDatabaseExistsAsync(IServiceProvider services, string databaseName)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("FoodSocial.Identity.Api.DatabaseInitialization");
    var connectionString = dbContext.Database.GetConnectionString()
        ?? throw new InvalidOperationException("Identity database connection string is not configured.");

    var adminConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Database = "postgres"
    }.ConnectionString;

    await using var connection = new NpgsqlConnection(adminConnectionString);
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"", StringComparison.Ordinal)}\";";

    try
    {
        await command.ExecuteNonQueryAsync();
        logger.LogInformation("Created PostgreSQL database {DatabaseName} for Identity.Api startup.", databaseName);
    }
    catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.DuplicateDatabase)
    {
        logger.LogInformation("PostgreSQL database {DatabaseName} already exists for Identity.Api startup.", databaseName);
    }
}

public partial class Program;

