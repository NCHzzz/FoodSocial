namespace FoodSocial.Identity.Domain;

/// <summary>
/// Represents the canonical lifecycle state for an identity account inside the Identity boundary.
/// </summary>
public enum IdentityLifecycleState
{
    PendingBootstrap = 0,
    Active = 1,
    Suspended = 2,
    Deleted = 3
}
