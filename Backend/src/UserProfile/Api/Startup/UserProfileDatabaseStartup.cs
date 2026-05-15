using FoodSocial.UserProfile.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodSocial.UserProfile.Api.Startup;

/// <summary>
/// Handles UserProfile API database startup tasks.
/// </summary>
public static class UserProfileDatabaseStartup
{
    /// <summary>
    /// Applies UserProfile EF Core migrations during startup.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

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

    /// <summary>
    /// Determines whether UserProfile migrations should run during startup.
    /// </summary>
    public static bool ShouldApplyMigrationsOnStartup(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool requireExplicitNonDevelopmentFlag = false)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

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
}
