namespace FoodSocial.Contracts.Identity;

/// <summary>
/// Wire-format string constants for identity lifecycle state and account roles.
/// </summary>
/// <remarks>
/// The Identity service serializes its <c>IdentityLifecycleState</c> enum and the
/// canonical <c>IdentityAppRoles</c> values as strings. Cross-service consumers
/// (BFF, Post, ...) must compare against these constants rather than hard-coded
/// literals so renames stay in sync with the producer.
/// </remarks>
public static class IdentityWireConstants
{
    /// <summary>Wire value for <c>IdentityLifecycleState.PendingBootstrap</c>.</summary>
    public const string LifecycleStatePendingBootstrap = "PendingBootstrap";

    /// <summary>Wire value for <c>IdentityLifecycleState.Active</c>.</summary>
    public const string LifecycleStateActive = "Active";

    /// <summary>Wire value for <c>IdentityLifecycleState.Suspended</c>.</summary>
    public const string LifecycleStateSuspended = "Suspended";

    /// <summary>Wire value for <c>IdentityLifecycleState.Deleted</c>.</summary>
    public const string LifecycleStateDeleted = "Deleted";

    /// <summary>Wire value for <c>IdentityAppRoles.User</c>.</summary>
    public const string RoleUser = "user";

    /// <summary>Wire value for <c>IdentityAppRoles.Admin</c>.</summary>
    public const string RoleAdmin = "admin";

    /// <summary>Wire value for <c>IdentityAppRoles.Moderator</c>.</summary>
    public const string RoleModerator = "moderator";
}
