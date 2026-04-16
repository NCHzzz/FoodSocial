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
            .Take(50)
            .ToListAsync(cancellationToken);

        var deliveredCount = 0;

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
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return deliveredCount;
    }
}
