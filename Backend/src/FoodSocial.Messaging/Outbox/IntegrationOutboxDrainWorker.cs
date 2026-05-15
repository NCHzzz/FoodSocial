using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodSocial.Messaging;

public sealed class IntegrationOutboxDrainWorker<TDbContext>(
    TDbContext dbContext,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<IntegrationOutboxDrainWorker<TDbContext>> logger)
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly IIntegrationEventPublisher _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    private readonly ILogger<IntegrationOutboxDrainWorker<TDbContext>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public int MaxRetryAttempts { get; set; } = 5;

    public async Task<int> DrainOnceAsync(CancellationToken cancellationToken)
    {
        var pendingQuery = _dbContext.Set<IntegrationOutboxMessage>()
            .Where(message =>
                message.DeliveryState == IntegrationOutboxDeliveryState.Pending
                || message.DeliveryState == IntegrationOutboxDeliveryState.FailedRetryable);

        var messages = IsSqliteProvider()
            ? (await pendingQuery.ToListAsync(cancellationToken))
                .OrderBy(message => message.AttemptCount)
                .ThenBy(message => message.CreatedAtUtc)
                .Take(50)
                .ToList()
            : await pendingQuery
                .OrderBy(message => message.AttemptCount)
                .ThenBy(message => message.CreatedAtUtc)
                .Take(50)
                .ToListAsync(cancellationToken);

        var deliveredCount = 0;

        foreach (var message in messages)
        {
            var attemptedAtUtc = DateTimeOffset.UtcNow;

            try
            {
                await _integrationEventPublisher.PublishAsync(message, cancellationToken);
                message.MarkDelivered(attemptedAtUtc);
                deliveredCount++;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Integration outbox delivery failed for {DbContext} message {OutboxMessageId} on attempt {AttemptCount}.",
                    typeof(TDbContext).Name,
                    message.Id,
                    message.AttemptCount + 1);
                message.MarkFailedRetryable(attemptedAtUtc, exception.Message, MaxRetryAttempts);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return deliveredCount;
    }

    private bool IsSqliteProvider() =>
        string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);
}
