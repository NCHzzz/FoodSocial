using FoodSocial.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodSocial.Identity.Infrastructure.Messaging;

/// <summary>
/// Drains pending identity outbox rows and publishes them to the messaging backbone.
/// </summary>
public sealed class IdentityOutboxDrainWorker(
    IdentityDbContext dbContext,
    IIdentityIntegrationEventPublisher integrationEventPublisher,
    ILogger<IdentityOutboxDrainWorker> logger)
{
    private const int BatchSize = 50;
    private readonly IdentityDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly IIdentityIntegrationEventPublisher _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    private readonly ILogger<IdentityOutboxDrainWorker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets or sets the maximum publish attempts before dead-letter state.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Drains one batch of pending and retryable messages.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of successfully delivered messages in this batch.</returns>
    public async Task<int> DrainOnceAsync(CancellationToken cancellationToken)
    {
        var messages = await _dbContext.IdentityOutboxMessages
            .Where(message =>
                message.DeliveryState == IdentityOutboxDeliveryState.Pending
                || message.DeliveryState == IdentityOutboxDeliveryState.FailedRetryable)
            .OrderBy(message => message.AttemptCount)
            .ThenBy(message => message.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var deliveredCount = 0;
        var failedCount = 0;
        var oldestPendingAgeSeconds = messages.Count == 0
            ? 0
            : Math.Max(
                0,
                (int)(DateTimeOffset.UtcNow - messages.Min(message => message.CreatedAtUtc)).TotalSeconds);

        foreach (var message in messages)
        {
            var attemptedAtUtc = DateTimeOffset.UtcNow;

            try
            {
                var payload = message.ReadPayload();
                await _integrationEventPublisher.PublishUserProvisionedAsync(payload, cancellationToken);
                message.MarkDelivered(attemptedAtUtc);
                deliveredCount++;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Identity outbox delivery failed for message {OutboxMessageId} on attempt {AttemptCount}.",
                    message.Id,
                    message.AttemptCount + 1);
                message.MarkFailedRetryable(attemptedAtUtc, exception.Message, MaxRetryAttempts);
                failedCount++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (messages.Count > 0)
        {
            _logger.LogInformation(
                "Identity outbox drain completed. BatchSize={BatchSize}, DeliveredCount={DeliveredCount}, FailedCount={FailedCount}, OldestPendingAgeSeconds={OldestPendingAgeSeconds}.",
                messages.Count,
                deliveredCount,
                failedCount,
                oldestPendingAgeSeconds);
        }

        return deliveredCount;
    }
}
