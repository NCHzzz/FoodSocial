namespace FoodSocial.Place.Domain;

/// <summary>
/// Represents the lifecycle status of a canonical Place.
/// </summary>
public enum PlaceLifecycleStatus
{
    /// <summary>The Place is active and visible in default search.</summary>
    Active,

    /// <summary>The Place is closed and omitted from default search.</summary>
    Closed,

    /// <summary>The Place is suspended and omitted from default search.</summary>
    Suspended
}
