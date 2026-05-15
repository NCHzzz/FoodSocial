using FoodSocial.Dish.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodSocial.Dish.Api.Startup;

/// <summary>
/// Handles Dish API database startup tasks.
/// </summary>
public static class DishDatabaseStartup
{
    /// <summary>
    /// Applies Dish EF Core migrations during startup.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

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

    /// <summary>
    /// Determines whether Dish migrations should run during startup.
    /// </summary>
    public static bool ShouldApplyMigrationsOnStartup(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool requireExplicitNonDevelopmentFlag = false)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var configuredValue = configuration.GetValue<bool?>("Dish:ApplyMigrationsOnStartup");
        if (configuredValue is not null)
        {
            return configuredValue.Value;
        }

        if (!environment.IsDevelopment() && requireExplicitNonDevelopmentFlag)
        {
            throw new InvalidOperationException("Dish:ApplyMigrationsOnStartup must be explicitly configured outside development.");
        }

        return environment.IsDevelopment();
    }
}
