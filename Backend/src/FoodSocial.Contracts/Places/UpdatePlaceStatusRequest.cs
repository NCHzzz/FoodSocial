namespace FoodSocial.Contracts.Places;

/// <summary>
/// Request contract for updating a canonical Place lifecycle status.
/// </summary>
public sealed record UpdatePlaceStatusRequest(string? Status);
