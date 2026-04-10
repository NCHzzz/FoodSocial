using FoodSocial.Identity.Domain;

namespace FoodSocial.Identity.Application;

/// <summary>
/// Provides application-facing lifecycle policy details for Identity workflows.
/// </summary>
public static class IdentityLifecyclePolicy
{
    /// <summary>
    /// Describes the canonical capabilities of the supplied lifecycle state.
    /// </summary>
    /// <param name="state">The lifecycle state to describe.</param>
    /// <returns>An application-facing policy description for the supplied state.</returns>
    public static IdentityLifecyclePolicyDescription Describe(IdentityLifecycleState state)
    {
        var rules = IdentityLifecycleRules.For(state);

        return new IdentityLifecyclePolicyDescription(
            CurrentState: rules.State,
            LoginAllowed: rules.LoginAllowed,
            RefreshAllowed: rules.RefreshAllowed,
            RoleResolutionAllowed: rules.RoleResolutionAllowed,
            DownstreamBootstrapPending: rules.DownstreamBootstrapPublicationRequired,
            Notes: BuildNotes(rules));
    }

    private static IReadOnlyList<string> BuildNotes(IdentityLifecycleRules rules)
    {
        return rules.State switch
        {
            IdentityLifecycleState.PendingBootstrap =>
            [
                "Pending bootstrap identities can sign in while downstream bootstrap remains outstanding.",
                "Pending bootstrap identities still require downstream bootstrap publication."
            ],
            IdentityLifecycleState.Active =>
            [
                "Active identities can sign in, refresh sessions, and resolve roles."
            ],
            IdentityLifecycleState.Suspended =>
            [
                "Suspended identities are blocked from login, refresh, and role resolution."
            ],
            IdentityLifecycleState.Deleted =>
            [
                "Deleted identities are terminal and cannot be reactivated by the lifecycle state machine."
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(rules), rules.State, "Unsupported lifecycle state.")
        };
    }
}
