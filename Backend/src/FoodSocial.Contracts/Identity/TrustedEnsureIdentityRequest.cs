namespace FoodSocial.Contracts.Identity;

/// <summary>
/// Represents a trusted internal identity ensure request issued by the BFF auth edge.
/// </summary>
/// <remarks>
/// The authenticated bearer token remains the sole source of truth for the ensured subject and trusted profile claims.
/// No caller-controlled subject field is accepted on the trusted internal ensure contract.
/// </remarks>
public sealed record TrustedEnsureIdentityRequest;
