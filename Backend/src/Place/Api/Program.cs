using FoodSocial.Place.Api;
using FoodSocial.Place.Api.Health;
using FoodSocial.Place.Application;
using FoodSocial.Place.Infrastructure;
using FoodSocial.Place.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<PlaceManagementAuthorizationFilter>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<PlacePostgresHealthCheck>("place-postgres");

var app = builder.Build();

var applyMigrationsOnStartup = ShouldApplyMigrationsOnStartup(builder.Configuration, builder.Environment, "Place", requireExplicitNonDevelopmentFlag: true);

if (applyMigrationsOnStartup)
{
    await ApplyPlaceDatabaseMigrationsAsync(app.Services);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();

static async Task ApplyPlaceDatabaseMigrationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PlaceDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("FoodSocial.Place.Api.DatabaseInitialization");
    var connectionString = dbContext.Database.GetConnectionString()
        ?? throw new InvalidOperationException("Place database connection string is not configured.");
    var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);

    logger.LogInformation(
        "Applying Place EF migrations against database {DatabaseName} on host {Host}:{Port}.",
        connectionBuilder.Database,
        connectionBuilder.Host,
        connectionBuilder.Port);

    await dbContext.Database.MigrateAsync();

    logger.LogInformation(
        "Finished applying Place EF migrations against database {DatabaseName}.",
        connectionBuilder.Database);
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
