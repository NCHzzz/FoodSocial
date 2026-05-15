using FoodSocial.Post.Api;
using FoodSocial.Post.Application;
using FoodSocial.Post.Infrastructure;
using FoodSocial.Post.Infrastructure.Persistence;
using FoodSocial.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

var realm = builder.Configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak realm is not configured.");
var keycloakAuthority = ResolveKeycloakAuthority(builder.Configuration, realm);
var requireHttpsMetadata = ResolveRequireHttpsMetadata(builder.Configuration, builder.Environment, keycloakAuthority);
var allowInsecureDevBackchannel = ShouldAllowInsecureDevBackchannel(builder.Environment, keycloakAuthority);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddScoped<PostInternalAuthorizationFilter>();
builder.Services.AddScoped<PostModeratorAuthorizationFilter>();
builder.Services.AddSingleton<PostMediaStorage>();
builder.Services.AddSingleton<PostApiMetrics>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<PostPostgresHealthCheck>("post-postgres")
    .AddCheck<RabbitMqHealthCheck>("post-rabbitmq");
builder.Services.AddHostedService<IntegrationOutboxPublisherBackgroundService<PostDbContext>>();
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
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = requireHttpsMetadata;
        options.MapInboundClaims = false;
        if (allowInsecureDevBackchannel)
        {
            options.BackchannelHttpHandler = CreateDevelopmentBackchannelHandler();
        }
    });

var app = builder.Build();

var applyMigrationsOnStartup = ShouldApplyMigrationsOnStartup(builder.Configuration, builder.Environment, "Post", requireExplicitNonDevelopmentFlag: true);
if (applyMigrationsOnStartup)
{
    await ApplyPostDatabaseMigrationsAsync(app.Services);
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

    var discoveredAuthority = configuration["Services:keycloak:http:0"]
        ?? configuration["Services:keycloak:https:0"]
        ?? throw new InvalidOperationException("Keycloak authority is not configured. Set Keycloak:Authority or provide an Aspire service reference for 'keycloak'.");

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

static async Task ApplyPostDatabaseMigrationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PostDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FoodSocial.Post.Api.DatabaseInitialization");
    var connectionString = dbContext.Database.GetConnectionString() ?? throw new InvalidOperationException("Post database connection string is not configured.");
    var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    logger.LogInformation("Applying Post EF migrations against database {DatabaseName} on host {Host}:{Port}.", connectionBuilder.Database, connectionBuilder.Host, connectionBuilder.Port);
    await dbContext.Database.MigrateAsync();
    logger.LogInformation("Finished applying Post EF migrations against database {DatabaseName}.", connectionBuilder.Database);
}

static bool ShouldApplyMigrationsOnStartup(
    IConfiguration configuration,
    IHostEnvironment environment,
    string serviceName,
    bool requireExplicitNonDevelopmentFlag = false)
{
    var configuredValue = configuration.GetValue<bool?>($"{serviceName}:ApplyMigrationsOnStartup");
    if (configuredValue is not null)
    {
        return configuredValue.Value;
    }

    if (!environment.IsDevelopment() && requireExplicitNonDevelopmentFlag)
    {
        throw new InvalidOperationException($"{serviceName}:ApplyMigrationsOnStartup must be explicitly configured outside development.");
    }

    return environment.IsDevelopment();
}

public partial class Program;
