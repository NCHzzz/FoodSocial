namespace FoodSocial.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken);
}
