namespace FoodSocial.Identity.Domain;

/// <summary>
/// Exposes canonical capabilities and transition rules for an identity lifecycle state.
/// </summary>
public sealed record IdentityLifecycleRules(
    IdentityLifecycleState State,
    bool LoginAllowed,
    bool RefreshAllowed,
    bool RoleResolutionAllowed,
    bool DownstreamBootstrapPublicationRequired)
{
    /// <summary>
    /// Gets the canonical rules for the supplied lifecycle state.
    /// </summary>
    /// <param name="state">The lifecycle state to describe.</param>
    /// <returns>The lifecycle rules for the supplied state.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the state is not defined.</exception>
    public static IdentityLifecycleRules For(IdentityLifecycleState state)
    {
        return state switch
        {
            IdentityLifecycleState.PendingBootstrap => new IdentityLifecycleRules(
                state,
                LoginAllowed: true,
                RefreshAllowed: true,
                RoleResolutionAllowed: true,
                DownstreamBootstrapPublicationRequired: true),
            IdentityLifecycleState.Active => new IdentityLifecycleRules(
                state,
                LoginAllowed: true,
                RefreshAllowed: true,
                RoleResolutionAllowed: true,
                DownstreamBootstrapPublicationRequired: false),
            IdentityLifecycleState.Suspended => new IdentityLifecycleRules(
                state,
                LoginAllowed: false,
                RefreshAllowed: false,
                RoleResolutionAllowed: false,
                DownstreamBootstrapPublicationRequired: false),
            IdentityLifecycleState.Deleted => new IdentityLifecycleRules(
                state,
                LoginAllowed: false,
                RefreshAllowed: false,
                RoleResolutionAllowed: false,
                DownstreamBootstrapPublicationRequired: false),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported lifecycle state.")
        };
    }

    /// <summary>
    /// Determines whether a transition is allowed from the supplied current state.
    /// </summary>
    /// <param name="currentState">The current lifecycle state.</param>
    /// <param name="transition">The requested lifecycle transition.</param>
    /// <returns><c>true</c> when the transition is allowed; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the state or transition is not defined.</exception>
    public static bool CanTransition(IdentityLifecycleState currentState, IdentityLifecycleTransition transition)
    {
        _ = For(currentState);

        return transition switch
        {
            IdentityLifecycleTransition.Activate => currentState is IdentityLifecycleState.PendingBootstrap or IdentityLifecycleState.Suspended,
            IdentityLifecycleTransition.Suspend => currentState is IdentityLifecycleState.PendingBootstrap or IdentityLifecycleState.Active,
            IdentityLifecycleTransition.Delete => currentState is not IdentityLifecycleState.Deleted,
            _ => throw new ArgumentOutOfRangeException(nameof(transition), transition, "Unsupported lifecycle transition.")
        };
    }

    /// <summary>
    /// Applies the supplied transition to the current state.
    /// </summary>
    /// <param name="currentState">The current lifecycle state.</param>
    /// <param name="transition">The requested lifecycle transition.</param>
    /// <returns>The next lifecycle state.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the transition is not allowed from the current state.</exception>
    public static IdentityLifecycleState Apply(IdentityLifecycleState currentState, IdentityLifecycleTransition transition)
    {
        if (!CanTransition(currentState, transition))
        {
            throw new InvalidOperationException($"Transition '{transition}' is not allowed from state '{currentState}'.");
        }

        return transition switch
        {
            IdentityLifecycleTransition.Activate => IdentityLifecycleState.Active,
            IdentityLifecycleTransition.Suspend => IdentityLifecycleState.Suspended,
            IdentityLifecycleTransition.Delete => IdentityLifecycleState.Deleted,
            _ => throw new ArgumentOutOfRangeException(nameof(transition), transition, "Unsupported lifecycle transition.")
        };
    }
}
