using FoodSocial.Place.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodSocial.Place.Api.Startup;

/// <summary>
/// Handles Place API database startup tasks.
/// </summary>
public static class PlaceDatabaseStartup
{
    /// <summary>
    /// Applies Place EF Core migrations during startup.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

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

    /// <summary>
    /// Determines whether Place migrations should run during startup.
    /// </summary>
    public static bool ShouldApplyMigrationsOnStartup(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool requireExplicitNonDevelopmentFlag = false)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var configuredValue = configuration.GetValue<bool?>("Place:ApplyMigrationsOnStartup");
        if (configuredValue is not null)
        {
            return configuredValue.Value;
        }

        if (!environment.IsDevelopment() && requireExplicitNonDevelopmentFlag)
        {
            throw new InvalidOperationException("Place:ApplyMigrationsOnStartup must be explicitly configured outside development.");
        }

        return environment.IsDevelopment();
    }
}
