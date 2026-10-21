using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodSocial.Identity.Infrastructure.Messaging;

/// <summary>
/// Background service that continuously drains the identity outbox.
/// </summary>
public sealed class IdentityOutboxPublisherBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<IdentityOutboxPublisherBackgroundService> logger)
    : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly ILogger<IdentityOutboxPublisherBackgroundService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Executes the background outbox-drain loop.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token used to stop the service.</param>
    /// <returns>A task that completes when the loop exits.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var drainWorker = scope.ServiceProvider.GetRequiredService<IdentityOutboxDrainWorker>();
                var deliveredCount = await drainWorker.DrainOnceAsync(stoppingToken);

                if (deliveredCount > 0)
                {
                    _logger.LogInformation("Identity outbox delivered {DeliveredCount} message(s) in this iteration.", deliveredCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Identity outbox drain iteration failed.");
            }

            var jitteredDelay = ComputeJitteredDelay(TimeSpan.FromSeconds(2));
            await Task.Delay(jitteredDelay, stoppingToken);
        }
    }

    private static TimeSpan ComputeJitteredDelay(TimeSpan baseDelay)
    {
        // Add +/- 25% jitter to avoid thundering herd when many service instances boot together.
        var jitterFactor = 0.75 + (Random.Shared.NextDouble() * 0.5);
        return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * jitterFactor);
    }
}
