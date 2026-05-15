using FoodSocial.Dish.Api;
using FoodSocial.Dish.Application;
using FoodSocial.Dish.Infrastructure;
using FoodSocial.Dish.Infrastructure.Persistence;
using FoodSocial.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddScoped<DishManagementAuthorizationFilter>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<DishPostgresHealthCheck>("dish-postgres")
    .AddCheck<RabbitMqHealthCheck>("dish-rabbitmq");
builder.Services.AddHostedService<IntegrationOutboxPublisherBackgroundService<DishDbContext>>();

var app = builder.Build();

var applyMigrationsOnStartup = ShouldApplyMigrationsOnStartup(builder.Configuration, builder.Environment, "Dish", requireExplicitNonDevelopmentFlag: true);

if (applyMigrationsOnStartup)
{
    await ApplyDishDatabaseMigrationsAsync(app.Services);
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

static async Task ApplyDishDatabaseMigrationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DishDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("FoodSocial.Dish.Api.DatabaseInitialization");
    var connectionString = dbContext.Database.GetConnectionString()
        ?? throw new InvalidOperationException("Dish database connection string is not configured.");
    var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);

    logger.LogInformation(
        "Applying Dish EF migrations against database {DatabaseName} on host {Host}:{Port}.",
        connectionBuilder.Database,
        connectionBuilder.Host,
        connectionBuilder.Port);

    await dbContext.Database.MigrateAsync();

    logger.LogInformation(
        "Finished applying Dish EF migrations against database {DatabaseName}.",
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
