using FoodSocial.Contracts.Identity;

namespace FoodSocial.UserProfile.Infrastructure.Messaging;

/// <summary>
/// Publishes explicit bootstrap completion signals back to the Identity boundary.
/// </summary>
public interface IUserProfileBootstrapCompletionPublisher
{
    /// <summary>
    /// Publishes a bootstrap completion signal.
    /// </summary>
    /// <param name="integrationEvent">The completion event payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the event has been published.</returns>
    Task PublishAsync(UserProfileBootstrapCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
