namespace FoodSocial.Identity.Domain;

/// <summary>
/// Represents the supported lifecycle transitions for an identity account.
/// </summary>
public enum IdentityLifecycleTransition
{
    Activate = 0,
    Suspend = 1,
    Delete = 2
}
