using FoodSocial.Place.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodSocial.Place.Api.Health;

/// <summary>
/// Checks whether the Place PostgreSQL database can be reached.
/// </summary>
public sealed class PlacePostgresHealthCheck(PlaceDbContext dbContext) : IHealthCheck
{
    private readonly PlaceDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Place PostgreSQL database is reachable.")
                : HealthCheckResult.Unhealthy("Place PostgreSQL database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Place PostgreSQL database health check failed.", exception);
        }
    }
}
