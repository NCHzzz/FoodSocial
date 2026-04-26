using FoodSocial.UserProfile.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodSocial.UserProfile.Api.Health;

/// <summary>
/// Verifies that UserProfile.Api can connect to its owned PostgreSQL database for readiness checks.
/// </summary>
public sealed class UserProfilePostgresHealthCheck(UserProfileDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("UserProfile PostgreSQL database is reachable.")
                : HealthCheckResult.Unhealthy("UserProfile PostgreSQL database is not reachable.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("UserProfile PostgreSQL readiness check failed.", exception);
        }
    }
}
