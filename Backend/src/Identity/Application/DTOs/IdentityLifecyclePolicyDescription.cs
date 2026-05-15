using FoodSocial.Identity.Domain;

namespace FoodSocial.Identity.Application.DTOs;

/// <summary>
/// Describes the application-facing capabilities of an identity lifecycle state.
/// </summary>
/// <param name="CurrentState">The current lifecycle state.</param>
/// <param name="LoginAllowed">Indicates whether login is allowed.</param>
/// <param name="RefreshAllowed">Indicates whether refresh is allowed.</param>
/// <param name="RoleResolutionAllowed">Indicates whether role resolution is allowed.</param>
/// <param name="DownstreamBootstrapPending">Indicates whether downstream bootstrap publication is still required.</param>
/// <param name="Notes">Human-readable notes for the lifecycle state.</param>
public sealed record IdentityLifecyclePolicyDescription(
    IdentityLifecycleState CurrentState,
    bool LoginAllowed,
    bool RefreshAllowed,
    bool RoleResolutionAllowed,
    bool DownstreamBootstrapPending,
    IReadOnlyList<string> Notes);
