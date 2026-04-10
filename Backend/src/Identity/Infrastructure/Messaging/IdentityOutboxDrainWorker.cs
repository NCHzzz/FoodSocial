using FoodSocial.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodSocial.Identity.Infrastructure.Messaging;

/// <summary>
/// Drains pending identity outbox rows and publishes them to the messaging backbone.
/// </summary>
public sealed class IdentityOutboxDrainWorker(
    IdentityDbContext dbContext,
    IIdentityIntegrationEventPublisher integrationEventPublisher)
{
    private readonly IdentityDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly IIdentityIntegrationEventPublisher _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));

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
                message.MarkFailedRetryable(attemptedAtUtc, exception.Message, MaxRetryAttempts);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return deliveredCount;
    }
}
