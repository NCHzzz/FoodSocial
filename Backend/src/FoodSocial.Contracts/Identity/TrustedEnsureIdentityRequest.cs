namespace FoodSocial.Contracts.Identity;

/// <summary>
/// Represents a trusted internal identity ensure request issued by the BFF auth edge.
/// </summary>
/// <remarks>
/// The authenticated bearer token identifies the trusted service caller.
/// The BFF supplies the browser-authenticated user claims that should be ensured.
/// </remarks>
public sealed record TrustedEnsureIdentityRequest(
    string Subject,
    string? PreferredUsername,
    string? Email);
