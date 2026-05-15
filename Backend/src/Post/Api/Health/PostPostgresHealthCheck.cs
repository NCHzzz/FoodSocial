using FoodSocial.Post.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodSocial.Post.Api.Health;

public sealed class PostPostgresHealthCheck(PostDbContext dbContext) : IHealthCheck
{
    private readonly PostDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect ? HealthCheckResult.Healthy("Post PostgreSQL database is reachable.") : HealthCheckResult.Unhealthy("Post PostgreSQL database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Post PostgreSQL database health check failed.", exception);
        }
    }
}
