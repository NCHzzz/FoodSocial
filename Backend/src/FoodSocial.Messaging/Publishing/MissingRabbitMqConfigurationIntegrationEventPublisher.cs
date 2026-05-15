namespace FoodSocial.Messaging;

public sealed class MissingRabbitMqConfigurationIntegrationEventPublisher(string serviceName) : IIntegrationEventPublisher
{
    private readonly string _serviceName = string.IsNullOrWhiteSpace(serviceName) ? "Service" : serviceName;

    public Task PublishAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        throw new InvalidOperationException($"{_serviceName} RabbitMQ connection string is not configured. Outbox message remains retryable until messaging is configured.");
    }
}
