using FoodSocial.Contracts.Identity;

namespace FoodSocial.Identity.Infrastructure.Messaging;

/// <summary>
/// Publishes identity integration events to the messaging backbone.
/// </summary>
public interface IIdentityIntegrationEventPublisher
{
    /// <summary>
    /// Publishes a user-provisioned integration event.
    /// </summary>
    /// <param name="integrationEvent">The event payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when publication has been attempted.</returns>
    Task PublishUserProvisionedAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
