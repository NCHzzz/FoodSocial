using FoodSocial.Post.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodSocial.Post.Api.Startup;

/// <summary>
/// Handles Post API database startup tasks.
/// </summary>
public static class PostDatabaseStartup
{
    /// <summary>
    /// Applies Post EF Core migrations during startup.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PostDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FoodSocial.Post.Api.DatabaseInitialization");
        var connectionString = dbContext.Database.GetConnectionString() ?? throw new InvalidOperationException("Post database connection string is not configured.");
        var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        logger.LogInformation("Applying Post EF migrations against database {DatabaseName} on host {Host}:{Port}.", connectionBuilder.Database, connectionBuilder.Host, connectionBuilder.Port);
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Finished applying Post EF migrations against database {DatabaseName}.", connectionBuilder.Database);
    }

    /// <summary>
    /// Determines whether Post migrations should run during startup.
    /// </summary>
    public static bool ShouldApplyMigrationsOnStartup(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool requireExplicitNonDevelopmentFlag = false)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var configuredValue = configuration.GetValue<bool?>("Post:ApplyMigrationsOnStartup");
        if (configuredValue is not null)
        {
            return configuredValue.Value;
        }

        if (!environment.IsDevelopment() && requireExplicitNonDevelopmentFlag)
        {
            throw new InvalidOperationException("Post:ApplyMigrationsOnStartup must be explicitly configured outside development.");
        }

        return environment.IsDevelopment();
    }
}
