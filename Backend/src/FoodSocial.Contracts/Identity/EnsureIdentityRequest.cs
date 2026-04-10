namespace FoodSocial.Contracts.Identity;

/// <summary>
/// Represents the authenticated ensure-identity request.
/// </summary>
/// <remarks>
/// The authenticated bearer token is the sole source of truth for subject and trusted profile claims.
/// Any caller-supplied JSON fields are ignored by design.
/// </remarks>
public sealed record EnsureIdentityRequest;
