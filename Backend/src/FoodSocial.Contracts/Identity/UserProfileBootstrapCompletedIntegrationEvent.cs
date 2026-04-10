namespace FoodSocial.Contracts.Identity;

/// <summary>
/// Represents the explicit completion signal emitted by UserProfile after a minimal shell exists.
/// </summary>
/// <param name="EventId">The unique completion event identifier.</param>
/// <param name="IdentityId">The local identity identifier.</param>
/// <param name="Subject">The trusted external identity subject.</param>
/// <param name="ProfileShellId">The created or ensured profile shell identifier.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when completion occurred.</param>
/// <param name="EventVersion">The integration event schema version.</param>
/// <param name="CorrelationId">The correlation identifier for cross-service tracing, when available.</param>
/// <param name="CausationId">The causation identifier for parent operation tracing, when available.</param>
public sealed record UserProfileBootstrapCompletedIntegrationEvent(
    Guid EventId,
    Guid IdentityId,
    string Subject,
    Guid ProfileShellId,
    DateTimeOffset OccurredAtUtc,
    int EventVersion,
    string? CorrelationId,
    string? CausationId);
