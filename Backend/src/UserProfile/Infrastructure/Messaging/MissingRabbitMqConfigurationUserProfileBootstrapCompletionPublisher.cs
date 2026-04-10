using FoodSocial.Contracts.Identity;

namespace FoodSocial.UserProfile.Infrastructure.Messaging;

/// <summary>
/// Throws a deterministic error when RabbitMQ completion publishing is not configured.
/// </summary>
public sealed class MissingRabbitMqConfigurationUserProfileBootstrapCompletionPublisher : IUserProfileBootstrapCompletionPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(UserProfileBootstrapCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        throw new InvalidOperationException(
            "RabbitMQ is not configured for UserProfile bootstrap completion publishing. Configure the 'rabbitmq' connection string before enabling bootstrap consumers.");
    }
}
