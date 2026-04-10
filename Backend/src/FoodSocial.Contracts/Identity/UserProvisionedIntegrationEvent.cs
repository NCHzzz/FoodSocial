namespace FoodSocial.Contracts.Identity;

/// <summary>
/// Represents the canonical integration event emitted when a local identity is provisioned for downstream bootstrap.
/// </summary>
/// <param name="EventId">The unique integration event identifier.</param>
/// <param name="IdentityId">The local identity identifier.</param>
/// <param name="Subject">The trusted external identity subject.</param>
/// <param name="PreferredUsername">The latest trusted preferred username value, when available.</param>
/// <param name="Email">The latest trusted email value, when available.</param>
/// <param name="Roles">The authoritative identity-owned role set at event creation time.</param>
/// <param name="LifecycleState">The identity lifecycle state when the event was created.</param>
/// <param name="DownstreamBootstrapPending"><c>true</c> when downstream bootstrap is still pending.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the event occurred.</param>
/// <param name="EventVersion">The integration event schema version.</param>
/// <param name="CorrelationId">The correlation identifier for cross-service tracing, when available.</param>
/// <param name="CausationId">The causation identifier for parent operation tracing, when available.</param>
public sealed record UserProvisionedIntegrationEvent(
    Guid EventId,
    Guid IdentityId,
    string Subject,
    string? PreferredUsername,
    string? Email,
    IReadOnlyList<string> Roles,
    string LifecycleState,
    bool DownstreamBootstrapPending,
    DateTimeOffset OccurredAtUtc,
    int EventVersion,
    string? CorrelationId,
    string? CausationId);
