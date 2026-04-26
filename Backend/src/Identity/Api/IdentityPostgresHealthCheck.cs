using FoodSocial.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodSocial.Identity.Api.Health;

/// <summary>
/// Verifies that Identity.Api can connect to its owned PostgreSQL database for readiness checks.
/// </summary>
public sealed class IdentityPostgresHealthCheck(IdentityDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Identity PostgreSQL database is reachable.")
                : HealthCheckResult.Unhealthy("Identity PostgreSQL database is not reachable.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Identity PostgreSQL readiness check failed.", exception);
        }
    }
}
