namespace FoodSocial.Messaging;

/// <summary>
/// Defines a RabbitMQ topology that supports bounded retry and dead-letter handling.
/// </summary>
public sealed record RabbitMqBoundedRetryTopology(
    string MainExchangeName,
    string QueueName,
    string RoutingKey,
    string RetryExchangeName,
    string RetryQueueName,
    string RetryRoutingKey,
    string DeadLetterExchangeName,
    string DeadLetterQueueName,
    string DeadLetterRoutingKey,
    int RetryDelayMilliseconds);
