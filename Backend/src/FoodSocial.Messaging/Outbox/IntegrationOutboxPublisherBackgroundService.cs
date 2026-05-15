using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodSocial.Messaging;

public sealed class IntegrationOutboxPublisherBackgroundService<TDbContext>(
    IServiceProvider serviceProvider,
    ILogger<IntegrationOutboxPublisherBackgroundService<TDbContext>> logger)
    : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<IntegrationOutboxPublisherBackgroundService<TDbContext>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Integration outbox background drain failed for {DbContext}.",
                    typeof(TDbContext).Name);
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task DrainOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var drainWorker = scope.ServiceProvider.GetRequiredService<IntegrationOutboxDrainWorker<TDbContext>>();
        await drainWorker.DrainOnceAsync(cancellationToken);
    }
}
