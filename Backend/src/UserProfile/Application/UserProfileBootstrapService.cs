using FoodSocial.Contracts.Identity;
using FoodSocial.UserProfile.Domain;

namespace FoodSocial.UserProfile.Application;

/// <summary>
/// Creates minimal profile shells idempotently from identity bootstrap events.
/// </summary>
public sealed class UserProfileBootstrapService(IUserProfileShellStore userProfileShellStore)
{
    private readonly IUserProfileShellStore _userProfileShellStore = userProfileShellStore ?? throw new ArgumentNullException(nameof(userProfileShellStore));

    /// <summary>
    /// Ensures a minimal profile shell exists for the supplied identity bootstrap event.
    /// </summary>
    /// <param name="integrationEvent">The identity bootstrap event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ensured profile shell and whether it was created during this call.</returns>
    public Task<UserProfileBootstrapResult> EnsureShellAsync(
        UserProvisionedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken) =>
        _userProfileShellStore.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                var existingShell = await _userProfileShellStore.FindByIdentityIdAsync(integrationEvent.IdentityId, transactionCancellationToken);
                if (existingShell is not null)
                {
                    return new UserProfileBootstrapResult(existingShell, Created: false);
                }

                var shell = new UserProfileShell(integrationEvent.IdentityId, integrationEvent.Subject);
                await _userProfileShellStore.AddAsync(shell, transactionCancellationToken);

                try
                {
                    await _userProfileShellStore.SaveChangesAsync(transactionCancellationToken);
                    return new UserProfileBootstrapResult(shell, Created: true);
                }
                catch (UserProfileBootstrapDuplicateIdentityException)
                {
                    var duplicateShell = await _userProfileShellStore.FindByIdentityIdAsync(integrationEvent.IdentityId, transactionCancellationToken)
                        ?? throw new InvalidOperationException(
                            $"The persistence boundary reported a duplicate UserProfile shell for identity '{integrationEvent.IdentityId}', but the existing shell could not be reloaded.");

                    return new UserProfileBootstrapResult(duplicateShell, Created: false);
                }
            },
            cancellationToken);
}

/// <summary>
/// Represents the result of profile shell bootstrap.
/// </summary>
/// <param name="Shell">The ensured shell.</param>
/// <param name="Created"><c>true</c> when the shell was created during this call.</param>
public sealed record UserProfileBootstrapResult(UserProfileShell Shell, bool Created);
