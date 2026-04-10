namespace FoodSocial.Identity.Api.Contracts;

/// <summary>
/// Represents a lightweight moderation-audit probe response.
/// </summary>
/// <param name="Authorized">Whether the caller passed moderator/admin policy checks.</param>
public sealed record ModerationAuditResponse(bool Authorized);
