using FoodSocial.Contracts.Identity;
using System.Text.Json;

namespace FoodSocial.Identity.Infrastructure.Persistence;

/// <summary>
/// Represents an identity-owned outbox record for reliable integration publication.
/// </summary>
public sealed class IdentityOutboxMessage
{
    /// <summary>
    /// Gets the outbox row identifier.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the logical event type name.
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the schema version for the payload.
    /// </summary>
    public int EventVersion { get; private set; }

    /// <summary>
    /// Gets the business identity identifier associated with this event.
    /// </summary>
    public Guid IdentityId { get; private set; }

    /// <summary>
    /// Gets the trusted external subject associated with this event.
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the serialized event payload.
    /// </summary>
    public string PayloadJson { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the durable delivery state.
    /// </summary>
    public IdentityOutboxDeliveryState DeliveryState { get; private set; } = IdentityOutboxDeliveryState.Pending;

    /// <summary>
    /// Gets the total publish attempts.
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this row was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the UTC timestamp of the most recent publish attempt, when present.
    /// </summary>
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when publication completed successfully, when present.
    /// </summary>
    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    /// <summary>
    /// Gets the last publish error text, when present.
    /// </summary>
    public string? LastError { get; private set; }

    private IdentityOutboxMessage()
    {
    }

    /// <summary>
    /// Creates a new pending outbox message from the provided integration event.
    /// </summary>
    /// <param name="integrationEvent">The integration event to persist.</param>
    /// <returns>The created outbox message.</returns>
    public static IdentityOutboxMessage Create(UserProvisionedIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var payloadJson = JsonSerializer.Serialize(integrationEvent);

        return new IdentityOutboxMessage
        {
            EventType = nameof(UserProvisionedIntegrationEvent),
            EventVersion = integrationEvent.EventVersion,
            IdentityId = integrationEvent.IdentityId,
            Subject = integrationEvent.Subject,
            PayloadJson = payloadJson,
            DeliveryState = IdentityOutboxDeliveryState.Pending,
            AttemptCount = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastAttemptAtUtc = null,
            DeliveredAtUtc = null,
            LastError = null
        };
    }

    /// <summary>
    /// Deserializes the event payload for publication.
    /// </summary>
    /// <returns>The event payload.</returns>
    public UserProvisionedIntegrationEvent ReadPayload()
    {
        var payload = JsonSerializer.Deserialize<UserProvisionedIntegrationEvent>(PayloadJson);
        return payload
            ?? throw new InvalidOperationException($"Outbox message '{Id}' payload could not be deserialized.");
    }

    /// <summary>
    /// Marks the message as delivered.
    /// </summary>
    /// <param name="attemptedAtUtc">The UTC timestamp when delivery was attempted.</param>
    public void MarkDelivered(DateTimeOffset attemptedAtUtc)
    {
        AttemptCount++;
        LastAttemptAtUtc = attemptedAtUtc;
        DeliveredAtUtc = attemptedAtUtc;
        DeliveryState = IdentityOutboxDeliveryState.Delivered;
        LastError = null;
    }

    /// <summary>
    /// Marks the message as failed and retryable.
    /// </summary>
    /// <param name="attemptedAtUtc">The UTC timestamp when delivery was attempted.</param>
    /// <param name="error">The publication failure message.</param>
    /// <param name="maxRetryAttempts">The maximum retry attempts before dead-lettering.</param>
    public void MarkFailedRetryable(DateTimeOffset attemptedAtUtc, string error, int maxRetryAttempts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetryAttempts);

        AttemptCount++;
        LastAttemptAtUtc = attemptedAtUtc;
        LastError = error;

        DeliveryState = AttemptCount >= maxRetryAttempts
            ? IdentityOutboxDeliveryState.DeadLetter
            : IdentityOutboxDeliveryState.FailedRetryable;
    }
}
