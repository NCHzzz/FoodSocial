using FoodSocial.Dish.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodSocial.Dish.Api;

/// <summary>
/// Checks whether the Dish PostgreSQL database can be reached.
/// </summary>
public sealed class DishPostgresHealthCheck(DishDbContext dbContext) : IHealthCheck
{
    private readonly DishDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Dish PostgreSQL database is reachable.")
                : HealthCheckResult.Unhealthy("Dish PostgreSQL database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Dish PostgreSQL database health check failed.", exception);
        }
    }
}
