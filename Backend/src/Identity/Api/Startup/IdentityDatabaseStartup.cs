using FoodSocial.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodSocial.Identity.Api.Startup;

/// <summary>
/// Handles Identity API database startup tasks.
/// </summary>
public static class IdentityDatabaseStartup
{
    /// <summary>
    /// Applies Identity EF Core migrations during startup.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    /// <summary>
    /// Determines whether Identity migrations should run during startup.
    /// </summary>
    public static bool ShouldApplyMigrationsOnStartup(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool requireExplicitNonDevelopmentFlag = false)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var configuredValue = configuration.GetValue<bool?>("Identity:ApplyMigrationsOnStartup");
        if (configuredValue is not null)
        {
            return configuredValue.Value;
        }

        if (!environment.IsDevelopment() && requireExplicitNonDevelopmentFlag)
        {
            throw new InvalidOperationException("Identity:ApplyMigrationsOnStartup must be explicitly configured outside development.");
        }

        return environment.IsDevelopment();
    }
}
