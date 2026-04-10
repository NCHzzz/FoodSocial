using FoodSocial.Contracts.Identity;
using FoodSocial.UserProfile.Domain;

namespace FoodSocial.UserProfile.Application;

/// <summary>
/// Defines persistence operations for idempotent profile shell bootstrap.
/// </summary>
public interface IUserProfileShellStore
{
    /// <summary>
    /// Finds an existing profile shell by identity identifier.
    /// </summary>
    /// <param name="identityId">The owning identity identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching shell, when present.</returns>
    Task<UserProfileShell?> FindByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new profile shell to the persistence boundary.
    /// </summary>
    /// <param name="shell">The shell to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the shell has been staged.</returns>
    Task AddAsync(UserProfileShell shell, CancellationToken cancellationToken);

    /// <summary>
    /// Saves staged changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when pending changes are persisted.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Executes an operation inside the local persistence transaction boundary.
    /// </summary>
    /// <typeparam name="TResult">The operation result type.</typeparam>
    /// <param name="operation">The transactional operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken);
}
