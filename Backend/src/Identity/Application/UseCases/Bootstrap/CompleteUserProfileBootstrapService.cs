using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Domain;

using FoodSocial.Identity.Application.Interfaces.Repositories;

namespace FoodSocial.Identity.Application.UseCases.Bootstrap;

/// <summary>
/// Applies explicit downstream bootstrap completion back into the Identity lifecycle state.
/// </summary>
public sealed class CompleteUserProfileBootstrapService(IIdentityUserStore identityUserStore)
{
    private readonly IIdentityUserStore _identityUserStore = identityUserStore ?? throw new ArgumentNullException(nameof(identityUserStore));

    /// <summary>
    /// Marks an identity active when UserProfile has completed bootstrap.
    /// </summary>
    /// <param name="integrationEvent">The explicit bootstrap completion signal.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when the lifecycle state changed; otherwise <c>false</c>.</returns>
    public Task<bool> CompleteAsync(UserProfileBootstrapCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        _identityUserStore.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                var identityUser = await _identityUserStore.FindByIdAsync(integrationEvent.IdentityId, transactionCancellationToken);
                if (identityUser is null)
                {
                    return false;
                }

                if (!string.Equals(identityUser.Subject, integrationEvent.Subject, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Bootstrap completion subject '{integrationEvent.Subject}' did not match identity '{integrationEvent.IdentityId}'.");
                }

                if (identityUser.State == IdentityLifecycleState.PendingBootstrap)
                {
                    identityUser.TransitionTo(IdentityLifecycleTransition.Activate);
                    await _identityUserStore.SaveChangesAsync(transactionCancellationToken);
                    return true;
                }

                return false;
            },
            cancellationToken);
}
