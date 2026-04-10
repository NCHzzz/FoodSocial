using FoodSocial.Contracts.Identity;

namespace FoodSocial.Identity.Infrastructure.Messaging;

/// <summary>
/// Failing publisher used when RabbitMQ configuration is missing.
/// </summary>
public sealed class MissingRabbitMqConfigurationIdentityIntegrationEventPublisher : IIdentityIntegrationEventPublisher
{
    private const string MissingConfigurationMessage =
        "RabbitMQ connection string is not configured for Identity integration event publication. Outbox message remains retryable until messaging is configured.";

    /// <inheritdoc />
    public Task PublishUserProvisionedAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        Task.FromException(new InvalidOperationException(MissingConfigurationMessage));
}
