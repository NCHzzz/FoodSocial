namespace FoodSocial.Contracts.Identity;

/// <summary>
/// Represents the local identity/bootstrap state attached to an authenticated BFF session.
/// </summary>
/// <param name="IdentityId">The local identity identifier.</param>
/// <param name="State">The current local identity lifecycle state.</param>
/// <param name="BootstrapPending"><c>true</c> when downstream bootstrap is still pending.</param>
/// <param name="DownstreamBootstrapPending"><c>true</c> when downstream bootstrap remains outstanding.</param>
public sealed record SessionIdentityDto(
    Guid IdentityId,
    string State,
    bool BootstrapPending,
    bool DownstreamBootstrapPending);
