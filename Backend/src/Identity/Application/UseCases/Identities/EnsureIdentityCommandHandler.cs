using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Application.DTOs;
using FoodSocial.Identity.Application.Exceptions;
using FoodSocial.Identity.Application.Interfaces.Repositories;
using FoodSocial.Identity.Domain;
using Microsoft.Extensions.Logging;

namespace FoodSocial.Identity.Application.UseCases.Identities;

/// <summary>
/// Ensures that a local identity exists for the authenticated subject.
/// </summary>
public sealed class EnsureIdentityCommandHandler(IIdentityUserStore identityUserStore, ILogger<EnsureIdentityCommandHandler> logger)
{
    private readonly IIdentityUserStore _identityUserStore = identityUserStore ?? throw new ArgumentNullException(nameof(identityUserStore));
    private readonly ILogger<EnsureIdentityCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Ensures a local identity mapping for the trusted authenticated subject.
    /// </summary>
    /// <param name="command">The trusted claim payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ensure-identity response.</returns>
    public async Task<EnsureIdentityResponse> HandleAsync(EnsureIdentityCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Subject);

        _logger.LogInformation("Ensuring local identity for subject '{Subject}'.", command.Subject);

        try
        {
            return await ExecuteEnsureTransactionAsync(command, cancellationToken);
        }
        catch (DuplicateIdentitySubjectException)
        {
            _logger.LogWarning(
                "Detected concurrent duplicate identity creation for subject '{Subject}'. Retrying in a clean persistence transaction.",
                command.Subject);

            return await ExecuteEnsureTransactionAsync(command, cancellationToken);
        }
    }

    private async Task WriteProvisionedOutboxMessageWhenRequiredAsync(
        IdentityUser identityUser,
        bool created,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identityUser);
        ArgumentNullException.ThrowIfNull(roleNames);

        var lifecyclePolicy = IdentityLifecyclePolicy.Describe(identityUser.State);

        if (!created && !lifecyclePolicy.DownstreamBootstrapPending)
        {
            _logger.LogInformation(
                "Skipping outbox staging for subject '{Subject}' because downstream bootstrap is already complete.",
                identityUser.Subject);
            return;
        }

        var integrationEvent = new UserProvisionedIntegrationEvent(
            EventId: Guid.NewGuid(),
            IdentityId: identityUser.Id,
            Subject: identityUser.Subject,
            PreferredUsername: identityUser.PreferredUsername,
            Email: identityUser.Email,
            Roles: roleNames,
            LifecycleState: lifecyclePolicy.CurrentState.ToString(),
            DownstreamBootstrapPending: lifecyclePolicy.DownstreamBootstrapPending,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            EventVersion: 1,
            CorrelationId: null,
            CausationId: null);

        await _identityUserStore.AddOutboxMessageAsync(integrationEvent, cancellationToken);
        _logger.LogInformation(
            "Staged identity bootstrap outbox message '{EventId}' for subject '{Subject}'. Created: {Created}.",
            integrationEvent.EventId,
            integrationEvent.Subject,
            created);
    }

    private async Task<(IdentityUser IdentityUser, bool Created)> EnsureIdentityUserAsync(
        EnsureIdentityCommand command,
        CancellationToken cancellationToken)
    {
        var identityUser = await _identityUserStore.FindBySubjectAsync(command.Subject, cancellationToken);

        if (identityUser is not null)
        {
            _logger.LogInformation("Found existing identity for subject '{Subject}'. Reconciling trusted claims.", command.Subject);
            identityUser.ReconcileProfile(command.PreferredUsername, command.Email);
            await _identityUserStore.SaveChangesAsync(cancellationToken);
            return (identityUser, false);
        }

        _logger.LogInformation("No existing identity found for subject '{Subject}'. Creating a new pending-bootstrap identity.", command.Subject);
        identityUser = new IdentityUser(command.Subject, command.PreferredUsername, command.Email);
        await _identityUserStore.AddAsync(identityUser, cancellationToken);

        await _identityUserStore.SaveChangesAsync(cancellationToken);
        return (identityUser, true);
    }

    private Task<EnsureIdentityResponse> ExecuteEnsureTransactionAsync(
        EnsureIdentityCommand command,
        CancellationToken cancellationToken)
    {
        return _identityUserStore.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                var (identityUser, created) = await EnsureIdentityUserAsync(command, transactionCancellationToken);
                _logger.LogInformation(
                    "Resolved local identity for subject '{Subject}' with identity id '{IdentityId}'. Created: {Created}. State: {State}.",
                    identityUser.Subject,
                    identityUser.Id,
                    created,
                    identityUser.State);

                _ = await _identityUserStore.EnsureRoleAssignedAsync(command.Subject, IdentityAppRoles.User, transactionCancellationToken);

                var roleNames = await _identityUserStore.GetRoleNamesBySubjectAsync(command.Subject, transactionCancellationToken);
                var normalizedRoleNames = roleNames
                    .Select(IdentityAppRoles.Normalize)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(role => role, StringComparer.Ordinal)
                    .ToArray();

                await WriteProvisionedOutboxMessageWhenRequiredAsync(identityUser, created, normalizedRoleNames, transactionCancellationToken);
                _logger.LogInformation(
                    "Persisting ensure-identity changes for subject '{Subject}'. Downstream bootstrap pending: {BootstrapPending}.",
                    identityUser.Subject,
                    IdentityLifecyclePolicy.Describe(identityUser.State).DownstreamBootstrapPending);

                await _identityUserStore.SaveChangesAsync(transactionCancellationToken);

                var lifecyclePolicy = IdentityLifecyclePolicy.Describe(identityUser.State);

                return new EnsureIdentityResponse(
                    identityUser.Id,
                    identityUser.Subject,
                    identityUser.PreferredUsername,
                    identityUser.Email,
                    normalizedRoleNames,
                    created,
                    lifecyclePolicy.CurrentState.ToString(),
                    lifecyclePolicy.DownstreamBootstrapPending);
            },
            cancellationToken);
    }
}
