namespace FoodSocial.Contracts.Identity;

/// <summary>
/// Represents the result of ensuring a local identity mapping for the authenticated subject.
/// </summary>
/// <param name="IdentityId">The local identity identifier.</param>
/// <param name="Subject">The trusted external subject from bearer claims.</param>
/// <param name="PreferredUsername">The cached preferred username sourced from trusted claims.</param>
/// <param name="Email">The cached email sourced from trusted claims.</param>
/// <param name="Roles">The application-owned roles resolved for the ensured identity.</param>
/// <param name="Created"><c>true</c> when the local identity was created during this request; otherwise <c>false</c>.</param>
/// <param name="State">The current identity lifecycle state.</param>
/// <param name="DownstreamBootstrapPending"><c>true</c> when downstream bootstrap is still pending.</param>
public sealed record EnsureIdentityResponse(
    Guid IdentityId,
    string Subject,
    string? PreferredUsername,
    string? Email,
    IReadOnlyList<string> Roles,
    bool Created,
    string State,
    bool DownstreamBootstrapPending);
