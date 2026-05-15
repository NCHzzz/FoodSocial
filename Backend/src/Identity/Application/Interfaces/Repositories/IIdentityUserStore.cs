using FoodSocial.Identity.Domain;
using FoodSocial.Contracts.Identity;

namespace FoodSocial.Identity.Application.Interfaces.Repositories;

/// <summary>
/// Defines the persistence operations required by the ensure-identity workflow.
/// </summary>
public interface IIdentityUserStore
{
    /// <summary>
    /// Finds an identity user by subject.
    /// </summary>
    /// <param name="subject">The trusted external subject.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching identity user when found; otherwise <c>null</c>.</returns>
    Task<IdentityUser?> FindBySubjectAsync(string subject, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new identity user to the persistence boundary.
    /// </summary>
    /// <param name="user">The identity user to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the identity has been staged for persistence.</returns>
    Task AddAsync(IdentityUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when pending changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets application-owned role names assigned to a subject.
    /// </summary>
    /// <param name="subject">The trusted subject.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The assigned role names.</returns>
    Task<IReadOnlyList<string>> GetRoleNamesBySubjectAsync(string subject, CancellationToken cancellationToken);

    /// <summary>
    /// Ensures an application-owned role is staged for a subject without committing the persistence unit.
    /// </summary>
    /// <param name="subject">The trusted subject.</param>
    /// <param name="role">The canonical role name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when a new role assignment was staged; otherwise <c>false</c>.</returns>
    Task<bool> EnsureRoleAssignedAsync(string subject, string role, CancellationToken cancellationToken);

    /// <summary>
    /// Finds an identity user by identifier.
    /// </summary>
    /// <param name="identityId">The identity identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching identity user when found; otherwise <c>null</c>.</returns>
    Task<IdentityUser?> FindByIdAsync(Guid identityId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds an integration event to the identity-owned outbox persistence boundary.
    /// </summary>
    /// <param name="integrationEvent">The integration event payload to persist durably.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the outbox message has been staged for persistence.</returns>
    Task AddOutboxMessageAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken);

    /// <summary>
    /// Executes the supplied operation inside the persistence transaction boundary.
    /// </summary>
    /// <typeparam name="TResult">The operation result type.</typeparam>
    /// <param name="operation">The operation to execute transactionally.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken);
}
