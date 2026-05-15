using System.Text.Json;

namespace FoodSocial.Messaging;

public sealed class IntegrationOutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public int EventVersion { get; private set; }
    public string ExchangeName { get; private set; } = string.Empty;
    public string RoutingKey { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }
    public string AggregateType { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public IntegrationOutboxDeliveryState DeliveryState { get; private set; } = IntegrationOutboxDeliveryState.Pending;
    public int AttemptCount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public string? LastError { get; private set; }

    private IntegrationOutboxMessage()
    {
    }

    public static IntegrationOutboxMessage Create<TEvent>(
        TEvent integrationEvent,
        Guid eventId,
        int eventVersion,
        DateTimeOffset occurredAtUtc,
        string? correlationId,
        string exchangeName,
        string routingKey,
        Guid aggregateId,
        string aggregateType)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        ArgumentOutOfRangeException.ThrowIfEqual(eventId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(aggregateId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);

        return new IntegrationOutboxMessage
        {
            EventId = eventId,
            EventType = typeof(TEvent).Name,
            EventVersion = eventVersion,
            ExchangeName = exchangeName,
            RoutingKey = routingKey,
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            CorrelationId = correlationId,
            OccurredAtUtc = occurredAtUtc,
            PayloadJson = JsonSerializer.Serialize(integrationEvent),
            DeliveryState = IntegrationOutboxDeliveryState.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void MarkDelivered(DateTimeOffset attemptedAtUtc)
    {
        AttemptCount++;
        LastAttemptAtUtc = attemptedAtUtc;
        DeliveredAtUtc = attemptedAtUtc;
        DeliveryState = IntegrationOutboxDeliveryState.Delivered;
        LastError = null;
    }

    public void MarkFailedRetryable(DateTimeOffset attemptedAtUtc, string error, int maxRetryAttempts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetryAttempts);

        AttemptCount++;
        LastAttemptAtUtc = attemptedAtUtc;
        LastError = error;
        DeliveryState = AttemptCount >= maxRetryAttempts
            ? IntegrationOutboxDeliveryState.DeadLetter
            : IntegrationOutboxDeliveryState.FailedRetryable;
    }
}
